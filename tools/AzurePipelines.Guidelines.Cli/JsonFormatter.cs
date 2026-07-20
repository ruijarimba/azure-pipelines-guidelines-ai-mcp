using System.Text.Json;
using System.Text.Json.Serialization;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Cli;

/// <summary>
/// Formats analysis results and guideline definitions as JSON.
/// </summary>
internal static class JsonFormatter
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    // ── Guidelines ────────────────────────────────────────────────────────────

    internal static string FormatGuidelineList(
        IReadOnlyList<GuidelineDefinition> guidelines,
        IGuidelineAutomationMetadataProvider? automationMetadataProvider = null)
    {
        GuidelineSummaryDto[] dtos = new GuidelineSummaryDto[guidelines.Count];
        for (int i = 0; i < guidelines.Count; i++)
        {
            GuidelineDefinition g = guidelines[i];
            GuidelineAutomationMetadata? metadata = automationMetadataProvider?.GetAutomationMetadata(g.Id);
            dtos[i] = new GuidelineSummaryDto(
                g.Id.Value,
                EnumToLower(g.Category),
                EnumToLower(g.Severity),
                g.Title,
                metadata is null ? null : EnumToLower(metadata.Status));
        }

        return JsonSerializer.Serialize(dtos, _options);
    }

    internal static string FormatGuidelineDetail(
        GuidelineDefinition g,
        IGuidelineAutomationMetadataProvider? automationMetadataProvider = null)
    {
        string[]? tags    = g.Tags.Count > 0 ? [.. g.Tags] : null;
        string[]? refs    = g.References.Count > 0 ? [.. g.References] : null;
        FixDto?   fix     = g.Fix is not null ? new FixDto(g.Fix.Summary, g.Fix.Before, g.Fix.After) : null;
        GuidelineAutomationMetadata? metadata = automationMetadataProvider?.GetAutomationMetadata(g.Id);

        GuidelineDetailDto dto = new(
            g.Id.Value,
            EnumToLower(g.Category),
            EnumToLower(g.Severity),
            g.Title,
            g.Description,
            g.Rationale,
            tags,
            fix,
            refs,
            metadata is null ? null : EnumToLower(metadata.Status),
            metadata?.Reason);

        return JsonSerializer.Serialize(dto, _options);
    }

    // ── Analysis results ──────────────────────────────────────────────────────

    internal static string Format(AnalysisResult result)
    {
        DiagnosticDto[] dtos = BuildDiagnosticDtos(result.Diagnostics);
        return JsonSerializer.Serialize(dtos, _options);
    }

    internal static string Format(IReadOnlyList<AnalysisResult> results)
    {
        FileAnalysisResultDto[] dtos = new FileAnalysisResultDto[results.Count];
        for (int i = 0; i < results.Count; i++)
        {
            AnalysisResult result = results[i];
            dtos[i] = new FileAnalysisResultDto(
                result.Document.FilePath,
                BuildDiagnosticDtos(result.Diagnostics));
        }

        return JsonSerializer.Serialize(dtos, _options);
    }

    private static DiagnosticDto[] BuildDiagnosticDtos(IReadOnlyList<Diagnostic> diagnostics)
    {
        DiagnosticDto[] dtos = new DiagnosticDto[diagnostics.Count];
        for (int i = 0; i < diagnostics.Count; i++)
        {
            Diagnostic d = diagnostics[i];
            dtos[i] = new DiagnosticDto(
                d.GuidelineId.Value,
                EnumToLower(d.Severity),
                d.Message,
                d.FilePath,
                d.Line);
        }

        return dtos;
    }

    /// <summary>Converts an enum value to lowercase ASCII without culture-sensitive processing.</summary>
    /// <param name="value">The enum value to convert.</param>
    /// <returns>The lowercase enum name.</returns>
    private static string EnumToLower<T>(T value) where T : struct, Enum
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

    // These records remain nested because they are private, formatter-specific JSON contracts.

    /// <summary>Represents a compact guideline summary in JSON output.</summary>
    private sealed record GuidelineSummaryDto(
        [property: JsonPropertyName("id")]       string Id,
        [property: JsonPropertyName("category")] string Category,
        [property: JsonPropertyName("severity")] string Severity,
        [property: JsonPropertyName("title")]    string Title,
        [property: JsonPropertyName("automationStatus")] string? AutomationStatus);

    /// <summary>Represents full guideline details in JSON output.</summary>
    private sealed record GuidelineDetailDto(
        [property: JsonPropertyName("id")]          string Id,
        [property: JsonPropertyName("category")]    string Category,
        [property: JsonPropertyName("severity")]    string Severity,
        [property: JsonPropertyName("title")]       string Title,
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("rationale")]   string? Rationale,
        [property: JsonPropertyName("tags")]        string[]? Tags,
        [property: JsonPropertyName("fix")]         FixDto? Fix,
        [property: JsonPropertyName("references")]  string[]? References,
        [property: JsonPropertyName("automationStatus")] string? AutomationStatus,
        [property: JsonPropertyName("automationReason")] string? AutomationReason);

    /// <summary>Represents fix guidance in JSON output.</summary>
    private sealed record FixDto(
        [property: JsonPropertyName("summary")] string Summary,
        [property: JsonPropertyName("before")]  string? Before,
        [property: JsonPropertyName("after")]   string? After);

    /// <summary>Represents one diagnostic in JSON output.</summary>
    private sealed record DiagnosticDto(
        [property: JsonPropertyName("ruleId")]   string RuleId,
        [property: JsonPropertyName("severity")] string Severity,
        [property: JsonPropertyName("message")]  string Message,
        [property: JsonPropertyName("filePath")] string FilePath,
        [property: JsonPropertyName("line")]     int? Line);

    /// <summary>Represents diagnostics grouped by source file in JSON output.</summary>
    private sealed record FileAnalysisResultDto(
        [property: JsonPropertyName("filePath")] string FilePath,
        [property: JsonPropertyName("diagnostics")] DiagnosticDto[] Diagnostics);
}

