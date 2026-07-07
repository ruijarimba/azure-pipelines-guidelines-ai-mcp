using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
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
internal sealed class PipelineAnalysisTools(IPipelineParser parser, IPipelineAnalyser analyser)
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
        "Pass an optional comma-separated list of guideline IDs to restrict analysis to " +
        "specific rules (e.g. \"ADOG-STEPS-001,ADOG-JOBS-006\").")]
    internal async Task<string> AnalyzePipelineAsync(
        [Description("The raw YAML content of the Azure Pipelines file to analyse.")]
        string yaml,
        [Description(
            "Optional comma-separated list of guideline IDs to check " +
            "(e.g. \"ADOG-STEPS-001,ADOG-JOBS-006\"). " +
            "Omit or pass null to run all rules.")]
        string? guidelineIds = null)
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

        AnalysisOptions options = BuildOptions(guidelineIds);
        AnalysisResult result = await analyser
            .AnalyseAsync(document, options)
            .ConfigureAwait(false);

        DiagnosticDto[] dtos = new DiagnosticDto[result.Diagnostics.Count];
        for (int i = 0; i < result.Diagnostics.Count; i++)
        {
            Diagnostic d = result.Diagnostics[i];
            dtos[i] = new DiagnosticDto(
                d.GuidelineId.Value,
                EnumToJsonString(d.Severity),
                d.Message,
                d.Line);
        }

        return JsonSerializer.Serialize(dtos, _jsonOptions);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static AnalysisOptions BuildOptions(string? guidelineIds)
    {
        if (string.IsNullOrWhiteSpace(guidelineIds))
        {
            return AnalysisOptions.Default;
        }

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

        return ids.Count > 0
            ? new AnalysisOptions(IncludedGuidelineIds: ids)
            : AnalysisOptions.Default;
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

    private sealed record ErrorResponse(
        [property: JsonPropertyName("error")] string Error);
}
