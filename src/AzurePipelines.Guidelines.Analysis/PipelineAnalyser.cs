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
    private readonly ILogger<PipelineAnalyser> _logger;

    public PipelineAnalyser(
        IEnumerable<IGuidelineRule> rules,
        IGuidelineRepository repository,
        ILogger<PipelineAnalyser> logger)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(logger);

        _rules = [.. rules];
        _repository = repository;
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

        IEnumerable<IGuidelineRule> applicableRules = FilterRules(_rules, options, _repository);

        List<Diagnostic> diagnostics = [];

        foreach (IGuidelineRule rule in applicableRules)
        {
            cancellationToken.ThrowIfCancellationRequested();

            LogEvaluatingRule(_logger, rule.GuidelineId.Value, document.FilePath);

            await foreach (Diagnostic diagnostic in rule.EvaluateAsync(document, cancellationToken))
            {
                if (diagnostic.Severity >= options.MinimumSeverity)
                {
                    diagnostics.Add(diagnostic);
                }
            }
        }

        LogAnalysisComplete(_logger, document.FilePath, diagnostics.Count);

        return new AnalysisResult(document, diagnostics);
    }

    private static IEnumerable<IGuidelineRule> FilterRules(
        IReadOnlyList<IGuidelineRule> rules,
        AnalysisOptions options,
        IGuidelineRepository repository)
    {
        IEnumerable<IGuidelineRule> filtered = rules;

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
}
