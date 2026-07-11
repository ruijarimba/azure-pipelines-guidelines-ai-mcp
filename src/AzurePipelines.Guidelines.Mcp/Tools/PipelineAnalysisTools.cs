using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using AzurePipelines.Guidelines.Analysis;
using AzurePipelines.Guidelines.Core;
using ModelContextProtocol.Server;

namespace AzurePipelines.Guidelines.Mcp.Tools;

/// <summary>
/// MCP tool handlers for analysing Azure Pipelines YAML against the loaded guidelines.
/// </summary>
[McpServerToolType]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance", "CA1812:Avoid uninstantiated internal classes",
    Justification = "Instantiated by the MCP SDK via dependency injection.")]
internal sealed class PipelineAnalysisTools(
    IPipelineParser parser,
    IPipelineAnalyser analyser,
    PipelinePathResolver pathResolver)
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    // ── analyze_pipeline ─────────────────────────────────────────────────────

    /// <summary>
    /// Analyses raw Azure Pipelines YAML content against the loaded guidelines
    /// and returns any violations found.
    /// </summary>
    [McpServerTool(Name = "analyze_pipeline")]
    [Description(
        "Analyses Azure Pipelines YAML content against the loaded guidelines and returns " +
        "a JSON array of violations. Each item includes the guideline ID, severity, message, " +
        "and the line number where the violation was detected. " +
        "Pass an optional category to restrict analysis to one guideline category, or an " +
        "optional comma-separated list of guideline IDs to restrict to specific rules.")]
    internal async Task<string> AnalyzePipelineAsync(
        [Description("The raw YAML content of the Azure Pipelines file to analyse.")]
        string yaml,
        [Description(
            "Optional comma-separated list of guideline IDs to check " +
            "(e.g. \"ADOG-STEPS-001,ADOG-JOBS-006\"). " +
            "Omit or pass null to run all rules.")]
        string? guidelineIds = null,
        [Description(
            "Optional category filter. " +
            "Allowed values: general, jobs, parameters, pipelines, stages, steps, variables. " +
            "Omit or pass null to include all categories.")]
        string? category = null)
    {
        if (string.IsNullOrWhiteSpace(yaml))
        {
            return JsonSerializer.Serialize(
                new ErrorResponse("Parameter 'yaml' is required."), _jsonOptions);
        }

        PipelineDocument document;
        try
        {
            document = parser.Parse(yaml, filePath: "(inline)");
        }
        catch (PipelineParsingException ex)
        {
            return JsonSerializer.Serialize(
                new ErrorResponse($"Failed to parse YAML: {ex.Message}"), _jsonOptions);
        }

        if (!TryBuildOptions(guidelineIds, category, out AnalysisOptions options, out string? optionsError))
        {
            return JsonSerializer.Serialize(new ErrorResponse(optionsError!), _jsonOptions);
        }

        AnalysisResult result = await analyser
            .AnalyseAsync(document, options)
            .ConfigureAwait(false);

        return JsonSerializer.Serialize(BuildDiagnosticDtos(result.Diagnostics), _jsonOptions);
    }

    // ── analyze_pipeline_paths ───────────────────────────────────────────────

    /// <summary>
    /// Analyses one or more Azure Pipelines YAML files or directories and returns any violations found.
    /// </summary>
    [McpServerTool(Name = "analyze_pipeline_paths")]
    [Description(
        "Analyses one or more Azure Pipelines YAML files or directories against the loaded guidelines " +
        "and returns aggregated violations. Directories are scanned recursively. " +
        "Pass an optional category to restrict analysis to one guideline category, or an " +
        "optional comma-separated list of guideline IDs to restrict to specific rules.")]
    internal async Task<string> AnalyzePipelinePathsAsync(
        [Description("One or more file or directory paths to analyse. Directories are scanned recursively.")]
        string[] paths,
        [Description(
            "Optional comma-separated list of guideline IDs to check " +
            "(e.g. \"ADOG-STEPS-001,ADOG-JOBS-006\"). Omit or pass null to run all rules.")]
        string? guidelineIds = null,
        [Description(
            "Optional category filter. " +
            "Allowed values: general, jobs, parameters, pipelines, stages, steps, variables. " +
            "Omit or pass null to include all categories.")]
        string? category = null)
    {
        if (paths is null || paths.Length == 0 || paths.All(string.IsNullOrWhiteSpace))
        {
            return JsonSerializer.Serialize(
                new ErrorResponse("Parameter 'paths' is required."), _jsonOptions);
        }

        IReadOnlyList<string> discoveredPaths;
        try
        {
            discoveredPaths = pathResolver.Resolve(paths);
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or ArgumentException)
        {
            return JsonSerializer.Serialize(new ErrorResponse(ex.Message), _jsonOptions);
        }

        if (!TryBuildOptions(guidelineIds, category, out AnalysisOptions options, out string? optionsError))
        {
            return JsonSerializer.Serialize(new ErrorResponse(optionsError!), _jsonOptions);
        }

        List<FileAnalysisResultDto> fileResults = [];

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
                    new ErrorResponse($"Cannot read file {discoveredPath}: {ex.Message}"), _jsonOptions);
            }

            PipelineDocument document;
            try
            {
                document = parser.Parse(yaml, discoveredPath);
            }
            catch (PipelineParsingException ex)
            {
                return JsonSerializer.Serialize(
                    new ErrorResponse($"Failed to parse YAML in {discoveredPath}: {ex.Message}"), _jsonOptions);
            }

            AnalysisResult result = await analyser
                .AnalyseAsync(document, options)
                .ConfigureAwait(false);

            fileResults.Add(new FileAnalysisResultDto(discoveredPath, BuildDiagnosticDtos(result.Diagnostics)));
        }

        return JsonSerializer.Serialize(fileResults, _jsonOptions);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool TryBuildOptions(
        string? guidelineIds,
        string? category,
        out AnalysisOptions options,
        out string? error)
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
                    error = $"Unknown category '{part}'. " +
                        "Allowed values: general, jobs, parameters, pipelines, stages, steps, variables.";
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
                    // Skip malformed IDs silently; the caller will see no results for them.
                }
            }

            if (ids.Count > 0)
            {
                includedIds = ids;
            }
        }

        options = includedCategories is null && includedIds is null
            ? AnalysisOptions.Default
            : new AnalysisOptions(
                IncludedCategories: includedCategories,
                IncludedGuidelineIds: includedIds);

        return true;
    }

    private static bool TryParseCategory(string value, out GuidelineCategory result)
    {
        result = value.ToUpperInvariant() switch
        {
            "GENERAL"    => GuidelineCategory.General,
            "JOBS"       => GuidelineCategory.Jobs,
            "PARAMETERS" => GuidelineCategory.Parameters,
            "PIPELINES"  => GuidelineCategory.Pipelines,
            "STAGES"     => GuidelineCategory.Stages,
            "STEPS"      => GuidelineCategory.Steps,
            "VARIABLES"  => GuidelineCategory.Variables,
            _            => (GuidelineCategory)(-1),
        };

        return (int)result >= 0;
    }

    private static DiagnosticDto[] BuildDiagnosticDtos(IReadOnlyList<Diagnostic> diagnostics)
    {
        DiagnosticDto[] dtos = new DiagnosticDto[diagnostics.Count];
        for (int i = 0; i < diagnostics.Count; i++)
        {
            Diagnostic d = diagnostics[i];
            dtos[i] = new DiagnosticDto(
                d.GuidelineId.Value,
                EnumToJsonString(d.Severity),
                d.Message,
                d.Line);
        }

        return dtos;
    }

    // ── Internal DTOs ─────────────────────────────────────────────────────────

    // Converts an enum value to a lowercase ASCII string for JSON output.
    // Avoids CA1308 (ToLowerInvariant) by using char arithmetic on ASCII enum names.
    private static string EnumToJsonString<T>(T value) where T : struct, Enum
    {
        string name = value.ToString();
        return string.Create(name.Length, name, static (span, src) =>
        {
            for (int i = 0; i < src.Length; i++)
            {
                char c = src[i];
                span[i] = c is >= 'A' and <= 'Z' ? (char)(c + 32) : c;
            }
        });
    }

    private sealed record DiagnosticDto(
        [property: JsonPropertyName("ruleId")] string RuleId,
        [property: JsonPropertyName("severity")] string Severity,
        [property: JsonPropertyName("message")] string Message,
        [property: JsonPropertyName("line")] int? Line);

    private sealed record FileAnalysisResultDto(
        [property: JsonPropertyName("filePath")] string FilePath,
        [property: JsonPropertyName("diagnostics")] DiagnosticDto[] Diagnostics);

    private sealed record ErrorResponse(
        [property: JsonPropertyName("error")] string Error);
}
