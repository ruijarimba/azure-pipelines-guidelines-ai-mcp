using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using AzurePipelines.Guidelines.Analysis;
using AzurePipelines.Guidelines.Core;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace AzurePipelines.Guidelines.Mcp.Tools;

/// <summary>
/// MCP tool handler for analysing Azure Pipelines YAML against the loaded guidelines.
/// </summary>
[McpServerToolType]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance", "CA1812:Avoid uninstantiated internal classes",
    Justification = "Instantiated by the MCP SDK via dependency injection.")]
internal sealed class AnalyzeTemplateOrFolderTool(
    IPipelineParser parser,
    IPipelineAnalyser analyser,
    PipelinePathResolver pathResolver,
    IGuidelineRepository repository,
    ILogger<AnalyzeTemplateOrFolderTool>? logger = null)
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Analyzes inline YAML or pipeline files discovered from a file or directory path.
    /// </summary>
    [McpServerTool(
        Name = "analyze_template_or_folder",
        Title = "Analyze pipeline, template, or folder",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = true)]
    [Description(
        "Analyses one Azure Pipelines pipeline or template, or all supported YAML files in a " +
        "file or directory path, against the loaded guidelines. Templates can define steps, " +
        "jobs, stages, or variables. Pass exactly one of yaml or fileOrPath. Directories are " +
        "scanned recursively. If a path cannot be resolved, common pipeline paths in the current " +
        "repository are tried automatically. Optional category and guideline ID filters restrict the rules checked.")]
    internal async Task<string> AnalyzeTemplateAsync(
        [Description("Inline YAML for one pipeline or template. Pass this or fileOrPath, not both.")]
        string? yaml = null,
        [Description("One file or directory path containing a pipeline or templates. Pass this or yaml, not both.")]
        string? fileOrPath = null,
        [Description(
            "Optional comma-separated list of guideline IDs to check " +
            "(e.g. \"ADOG-STEPS-001,ADOG-JOBS-006\"). " +
            "Omit or pass null to run enforceable rules only.")]
        string? guidelineIds = null,
        [Description(
            "Optional category filter. " +
            "Allowed values: general, jobs, parameters, pipelines, stages, steps, variables. " +
            "Omit or pass null to include all categories.")]
        string? category = null,
        [Description(
            "When true, includes heuristic and non-automatable rules in addition to enforceable rules. " +
            "Defaults to false (enforceable rules only). Ignored when guidelineIds is provided.")]
        bool includeNonEnforceable = false)
    {
        ILogger<AnalyzeTemplateOrFolderTool> invocationLogger =
            logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<AnalyzeTemplateOrFolderTool>.Instance;

        bool hasYaml = !string.IsNullOrWhiteSpace(yaml);
        bool hasPath = !string.IsNullOrWhiteSpace(fileOrPath);
        if (hasYaml == hasPath)
        {
            return JsonSerializer.Serialize(
                new ErrorResponseDto("Pass exactly one of 'yaml' or 'fileOrPath'."), _jsonOptions);
        }

        if (!TryBuildOptions(
            guidelineIds,
            category,
            includeNonEnforceable,
            out AnalysisOptions options,
            out string? optionsError))
        {
            return JsonSerializer.Serialize(new ErrorResponseDto(optionsError!), _jsonOptions);
        }

        McpToolInvocationLog.Log(invocationLogger, "analyze_template_or_folder", category, guidelineIds, options);

        if (hasYaml)
        {
            return await AnalyzeInlineAsync(yaml!, options).ConfigureAwait(false);
        }

        if (!pathResolver.TryResolveWithRepositoryFallback(
            fileOrPath!,
            out IReadOnlyList<string> discoveredPaths,
            out _,
            out string? pathError))
        {
            return JsonSerializer.Serialize(new ErrorResponseDto(pathError!), _jsonOptions);
        }

        return await AnalyzeFilesAsync(discoveredPaths, options).ConfigureAwait(false);
    }

    private async Task<string> AnalyzeInlineAsync(string yaml, AnalysisOptions options)
    {
        PipelineDocument document;
        try
        {
            document = parser.Parse(yaml, filePath: "(inline)");
        }
        catch (PipelineParsingException ex)
        {
            return JsonSerializer.Serialize(
                new ErrorResponseDto($"Failed to parse YAML: {ex.Message}"),
                _jsonOptions);
        }

        AnalysisResult result = await analyser.AnalyseAsync(document, options).ConfigureAwait(false);
        DiagnosticDto[] diagnostics = BuildDiagnosticDtos(result.Diagnostics);
        return JsonSerializer.Serialize(
            new AnalysisResponse(BuildSummary([result]), diagnostics, null), _jsonOptions);
    }

    private async Task<string> AnalyzeFilesAsync(IReadOnlyList<string> discoveredPaths, AnalysisOptions options)
    {
        List<FileAnalysisResultDto> fileResults = [];
        List<AnalysisResult> results = [];

        foreach (string discoveredPath in discoveredPaths)
        {
            string yaml;
            try
            {
                yaml = await File.ReadAllTextAsync(discoveredPath).ConfigureAwait(false);
            }
            catch (IOException ex)
            {
                return JsonSerializer.Serialize(
                    new ErrorResponseDto($"Cannot read file {discoveredPath}: {ex.Message}"), _jsonOptions);
            }

            PipelineDocument document;
            try
            {
                document = parser.Parse(yaml, discoveredPath);
            }
            catch (PipelineParsingException ex)
            {
                return JsonSerializer.Serialize(
                    new ErrorResponseDto($"Failed to parse YAML in {discoveredPath}: {ex.Message}"), _jsonOptions);
            }

            AnalysisResult result = await analyser.AnalyseAsync(document, options).ConfigureAwait(false);
            results.Add(result);
            fileResults.Add(new FileAnalysisResultDto(discoveredPath, BuildDiagnosticDtos(result.Diagnostics)));
        }

        return JsonSerializer.Serialize(new AnalysisResponse(BuildSummary(results), null, fileResults), _jsonOptions);
    }

    private static bool TryBuildOptions(string? guidelineIds, string? category, bool includeNonEnforceable,
        out AnalysisOptions options, out string? error)
    {
        error = null;
        IReadOnlyList<GuidelineCategory>? includedCategories = null;
        if (!string.IsNullOrWhiteSpace(category))
        {
            List<GuidelineCategory> parsedCategories = [];
            foreach (string part in category.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                if (!TryParseCategory(part, out GuidelineCategory parsedCategory))
                {
                    options = AnalysisOptions.Default;
                    error = $"Unknown category '{part}'. Allowed values: general, jobs, parameters, pipelines, stages, steps, variables.";
                    return false;
                }
                parsedCategories.Add(parsedCategory);
            }
            includedCategories = parsedCategories.Distinct().ToArray();
        }

        IReadOnlyList<GuidelineId>? includedIds = null;
        if (!string.IsNullOrWhiteSpace(guidelineIds))
        {
            List<GuidelineId> ids = [];
            foreach (string part in guidelineIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                try
                {
                    ids.Add(new GuidelineId(part));
                }
                catch (ArgumentException)
                {
                }
            }
            if (ids.Count > 0)
            {
                includedIds = ids;
            }
        }

        options = new AnalysisOptions(
            IncludedCategories: includedCategories,
            IncludedGuidelineIds: includedIds,
            EnforceableOnly: !includeNonEnforceable);
        return true;
    }

    private static bool TryParseCategory(string value, out GuidelineCategory result)
    {
        result = value.ToUpperInvariant() switch
        {
            "GENERAL" => GuidelineCategory.General,
            "JOBS" => GuidelineCategory.Jobs,
            "PARAMETERS" => GuidelineCategory.Parameters,
            "PIPELINES" => GuidelineCategory.Pipelines,
            "STAGES" => GuidelineCategory.Stages,
            "STEPS" => GuidelineCategory.Steps,
            "VARIABLES" => GuidelineCategory.Variables,
            _ => (GuidelineCategory)(-1),
        };
        return (int)result >= 0;
    }

    private DiagnosticDto[] BuildDiagnosticDtos(IReadOnlyList<Diagnostic> diagnostics)
    {
        DiagnosticDto[] dtos = new DiagnosticDto[diagnostics.Count];
        for (int i = 0; i < diagnostics.Count; i++)
        {
            Diagnostic d = diagnostics[i];
            dtos[i] = new DiagnosticDto(d.GuidelineId.Value, ResolveRecommendation(d), d.Message, d.Line);
        }
        return dtos;
    }

    private string ResolveRecommendation(Diagnostic diagnostic)
    {
        GuidelineDefinition? def = repository.FindById(diagnostic.GuidelineId);
        return def is not null
            ? EnumToJsonString(def.Severity)
            : diagnostic.Severity switch
            {
                DiagnosticSeverity.Error => EnumToJsonString(GuidelineSeverity.Do),
                DiagnosticSeverity.Warning => EnumToJsonString(GuidelineSeverity.Avoid),
                _ => EnumToJsonString(GuidelineSeverity.Consider),
            };
    }

    private AnalysisSummaryDto BuildSummary(List<AnalysisResult> results)
    {
        Dictionary<string, int> byRecommendation = [];
        Dictionary<string, int> byCategory = [];
        Dictionary<string, int> byRule = [];
        int filesWithFindings = 0;
        int totalFindings = 0;
        foreach (AnalysisResult result in results)
        {
            if (result.Diagnostics.Count > 0)
            {
                filesWithFindings++;
            }
            totalFindings += result.Diagnostics.Count;
            foreach (Diagnostic diagnostic in result.Diagnostics)
            {
                GuidelineDefinition? guideline = repository.FindById(diagnostic.GuidelineId);
                Increment(byRecommendation, ResolveRecommendation(diagnostic));
                Increment(byRule, diagnostic.GuidelineId.Value);
                if (guideline is not null)
                {
                    Increment(byCategory, EnumToJsonString(guideline.Category));
                }
            }
        }
        return new AnalysisSummaryDto(results.Count, filesWithFindings, totalFindings,
            byRecommendation.Count == 0 ? null : new SortedDictionary<string, int>(byRecommendation),
            byCategory.Count == 0 ? null : new SortedDictionary<string, int>(byCategory),
            byRule.Count == 0 ? null : new SortedDictionary<string, int>(byRule));
    }

    private static void Increment(Dictionary<string, int> counts, string key) =>
        counts[key] = counts.TryGetValue(key, out int count) ? count + 1 : 1;

    private sealed record DiagnosticDto(
        [property: JsonPropertyName("ruleId")] string RuleId,
        [property: JsonPropertyName("recommendation")] string Recommendation,
        [property: JsonPropertyName("message")] string Message,
        [property: JsonPropertyName("line")] int? Line);

    private sealed record FileAnalysisResultDto(
        [property: JsonPropertyName("filePath")] string FilePath,
        [property: JsonPropertyName("diagnostics")] DiagnosticDto[] Diagnostics);

    private sealed record AnalysisResponse(
        [property: JsonPropertyName("summary")] AnalysisSummaryDto Summary,
        [property: JsonPropertyName("diagnostics")] DiagnosticDto[]? Diagnostics,
        [property: JsonPropertyName("files")] List<FileAnalysisResultDto>? Files);

    private sealed record AnalysisSummaryDto(
        [property: JsonPropertyName("filesAnalyzed")] int FilesAnalyzed,
        [property: JsonPropertyName("filesWithFindings")] int FilesWithFindings,
        [property: JsonPropertyName("totalFindings")] int TotalFindings,
        [property: JsonPropertyName("byRecommendation")] SortedDictionary<string, int>? ByRecommendation,
        [property: JsonPropertyName("byCategory")] SortedDictionary<string, int>? ByCategory,
        [property: JsonPropertyName("byRule")] SortedDictionary<string, int>? ByRule);

    private static string EnumToJsonString<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        string name = value.ToString();
        return string.Create(name.Length, name, static (span, source) =>
        {
            for (int i = 0; i < source.Length; i++)
            {
                char c = source[i];
                span[i] = c is >= 'A' and <= 'Z' ? (char)(c + 32) : c;
            }
        });
    }
}
