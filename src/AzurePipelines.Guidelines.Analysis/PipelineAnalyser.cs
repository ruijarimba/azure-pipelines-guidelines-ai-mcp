using AzurePipelines.Guidelines.Core;
using Microsoft.Extensions.Logging;

namespace AzurePipelines.Guidelines.Analysis;

/// <summary>
/// Runs all registered <see cref="IGuidelineRule"/> implementations against a
/// <see cref="PipelineDocument"/> and aggregates the results into an
/// <see cref="AnalysisResult"/>.
/// </summary>
internal sealed class PipelineAnalyser : IPipelineAnalyser
{
    private static readonly Action<ILogger, string, string, Exception?> _logEvaluatingRule =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new EventId(1, nameof(LogEvaluatingRule)),
            "Evaluating rule {RuleId} against {FilePath}");

    private static readonly Action<ILogger, string, int, Exception?> _logAnalysisComplete =
        LoggerMessage.Define<string, int>(
            LogLevel.Debug,
            new EventId(2, nameof(LogAnalysisComplete)),
            "Analysis of {FilePath} complete: {Count} diagnostic(s)");

    private static void LogEvaluatingRule(ILogger logger, string ruleId, string filePath)
        => _logEvaluatingRule(logger, ruleId, filePath, null);

    private static void LogAnalysisComplete(ILogger logger, string filePath, int count)
        => _logAnalysisComplete(logger, filePath, count, null);

    private readonly IReadOnlyList<IGuidelineRule> _rules;
    private readonly IGuidelineRepository _repository;
    private readonly IGuidelineAutomationMetadataProvider _automationMetadataProvider;
    private readonly IPipelineSchemaValidator _schemaValidator;
    private readonly ILogger<PipelineAnalyser> _logger;

    public PipelineAnalyser(
        IEnumerable<IGuidelineRule> rules,
        IGuidelineRepository repository,
        IGuidelineAutomationMetadataProvider automationMetadataProvider,
        IPipelineSchemaValidator schemaValidator,
        ILogger<PipelineAnalyser> logger)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(automationMetadataProvider);
        ArgumentNullException.ThrowIfNull(schemaValidator);
        ArgumentNullException.ThrowIfNull(logger);

        _rules = [.. rules];
        _repository = repository;
        _automationMetadataProvider = automationMetadataProvider;
        _schemaValidator = schemaValidator;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<AnalysisResult> AnalyseAsync(
        PipelineDocument document,
        AnalysisOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        options ??= AnalysisOptions.Default;

        IReadOnlyList<IGuidelineRule> applicableRules = FilterRules(
            _rules,
            options,
            _repository,
            _automationMetadataProvider)
            .ToArray();
        IReadOnlyList<SkippedGuideline> skippedGuidelines = GetSkippedGuidelines(
            _rules,
            applicableRules,
            options,
            _automationMetadataProvider);

        List<Diagnostic> diagnostics = [];
        IReadOnlyList<SchemaDiagnostic> schemaDiagnostics = _schemaValidator.Validate(
            document.RawContent,
            document.FilePath,
            PipelineSchemaContext.Pipeline);

        foreach (IGuidelineRule rule in applicableRules)
        {
            cancellationToken.ThrowIfCancellationRequested();

            LogEvaluatingRule(_logger, rule.GuidelineId.Value, document.FilePath);

            await foreach (Diagnostic diagnostic in rule.EvaluateAsync(document, cancellationToken)
                .ConfigureAwait(false))
            {
                bool includeBySeverity = options.IncludedDiagnosticSeverities is { Count: > 0 }
                    ? options.IncludedDiagnosticSeverities.Contains(diagnostic.Severity)
                    : diagnostic.Severity >= options.MinimumSeverity;

                if (includeBySeverity)
                {
                    diagnostics.Add(diagnostic);
                }
            }
        }

        LogAnalysisComplete(_logger, document.FilePath, diagnostics.Count);

        return new AnalysisResult(document, diagnostics, schemaDiagnostics, skippedGuidelines);
    }

    private static IEnumerable<IGuidelineRule> FilterRules(
        IReadOnlyList<IGuidelineRule> rules,
        AnalysisOptions options,
        IGuidelineRepository repository,
        IGuidelineAutomationMetadataProvider automationMetadataProvider)
    {
        IEnumerable<IGuidelineRule> filtered = rules.Where(rule => IsEnabled(rule, options, automationMetadataProvider));

        if (options.IncludedCategories is { Count: > 0 })
        {
            HashSet<GuidelineCategory> categories = [.. options.IncludedCategories];
            filtered = filtered.Where(r =>
            {
                GuidelineDefinition? def = repository.FindById(r.GuidelineId);
                return def is not null && categories.Contains(def.Category);
            });
        }

        if (options.IncludedGuidelineIds is { Count: > 0 })
        {
            HashSet<string> ids = options.IncludedGuidelineIds
                .Select(id => id.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            filtered = filtered.Where(r => ids.Contains(r.GuidelineId.Value));
        }

        return filtered;
    }

    private static bool IsEnabled(
        IGuidelineRule rule,
        AnalysisOptions options,
        IGuidelineAutomationMetadataProvider automationMetadataProvider)
    {
        GuidelineAutomationMetadata? metadata = automationMetadataProvider.GetAutomationMetadata(rule.GuidelineId);
        return metadata?.Status switch
        {
            GuidelineAutomationStatus.Enforceable => true,
            GuidelineAutomationStatus.Heuristic => options.IncludeHeuristics,
            GuidelineAutomationStatus.NotAutomatable => false,
            null => false,
            _ => false,
        };
    }

    private static List<SkippedGuideline> GetSkippedGuidelines(
        IReadOnlyList<IGuidelineRule> rules,
        IReadOnlyList<IGuidelineRule> applicableRules,
        AnalysisOptions options,
        IGuidelineAutomationMetadataProvider automationMetadataProvider)
    {
        HashSet<GuidelineId> evaluatedIds = [.. applicableRules.Select(rule => rule.GuidelineId)];
        List<SkippedGuideline> skipped = [];

        foreach (IGuidelineRule rule in rules)
        {
            GuidelineAutomationMetadata? metadata = automationMetadataProvider.GetAutomationMetadata(rule.GuidelineId);
            if (evaluatedIds.Contains(rule.GuidelineId) || metadata is null)
            {
                continue;
            }

            if (metadata.Status is GuidelineAutomationStatus.NotAutomatable ||
                (metadata.Status is GuidelineAutomationStatus.Heuristic && !options.IncludeHeuristics))
            {
                skipped.Add(new SkippedGuideline(rule.GuidelineId, metadata.Status, metadata.Reason));
            }
        }

        return skipped;
    }
}
