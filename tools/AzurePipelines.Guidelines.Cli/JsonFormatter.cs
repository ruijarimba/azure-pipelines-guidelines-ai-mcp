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

    internal static string FormatGuidelineList(IReadOnlyList<GuidelineDefinition> guidelines)
    {
        GuidelineSummaryDto[] dtos = new GuidelineSummaryDto[guidelines.Count];
        for (int i = 0; i < guidelines.Count; i++)
        {
            GuidelineDefinition g = guidelines[i];
            dtos[i] = new GuidelineSummaryDto(
                g.Id.Value,
                EnumToLower(g.Category),
                EnumToLower(g.Severity),
                g.Title);
        }

        return JsonSerializer.Serialize(dtos, _options);
    }

    internal static string FormatGuidelineDetail(GuidelineDefinition g)
    {
        string[]? tags    = g.Tags.Count > 0 ? [.. g.Tags] : null;
        string[]? refs    = g.References.Count > 0 ? [.. g.References] : null;
        FixDto?   fix     = g.Fix is not null ? new FixDto(g.Fix.Summary, g.Fix.Before, g.Fix.After) : null;

        GuidelineDetailDto dto = new(
            g.Id.Value,
            EnumToLower(g.Category),
            EnumToLower(g.Severity),
            g.Title,
            g.Description,
            g.Rationale,
            tags,
            fix,
            refs);

        return JsonSerializer.Serialize(dto, _options);
    }

    // ── Analysis results ──────────────────────────────────────────────────────

    internal static string Format(AnalysisResult result)
    {
        DiagnosticDto[] dtos = new DiagnosticDto[result.Diagnostics.Count];
        for (int i = 0; i < result.Diagnostics.Count; i++)
        {
            Diagnostic d = result.Diagnostics[i];
            dtos[i] = new DiagnosticDto(
                d.GuidelineId.Value,
                EnumToLower(d.Severity),
                d.Message,
                d.FilePath,
                d.Line);
        }

        return JsonSerializer.Serialize(dtos, _options);
    }

    // Converts an enum value to lowercase ASCII — avoids CA1308 (ToLowerInvariant).
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

    // ── DTOs ──────────────────────────────────────────────────────────────────

    private sealed record GuidelineSummaryDto(
        [property: JsonPropertyName("id")]       string Id,
        [property: JsonPropertyName("category")] string Category,
        [property: JsonPropertyName("severity")] string Severity,
        [property: JsonPropertyName("title")]    string Title);

    private sealed record GuidelineDetailDto(
        [property: JsonPropertyName("id")]          string Id,
        [property: JsonPropertyName("category")]    string Category,
        [property: JsonPropertyName("severity")]    string Severity,
        [property: JsonPropertyName("title")]       string Title,
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("rationale")]   string? Rationale,
        [property: JsonPropertyName("tags")]        string[]? Tags,
        [property: JsonPropertyName("fix")]         FixDto? Fix,
        [property: JsonPropertyName("references")]  string[]? References);

    private sealed record FixDto(
        [property: JsonPropertyName("summary")] string Summary,
        [property: JsonPropertyName("before")]  string? Before,
        [property: JsonPropertyName("after")]   string? After);

    private sealed record DiagnosticDto(
        [property: JsonPropertyName("ruleId")]   string RuleId,
        [property: JsonPropertyName("severity")] string Severity,
        [property: JsonPropertyName("message")]  string Message,
        [property: JsonPropertyName("filePath")] string FilePath,
        [property: JsonPropertyName("line")]     int? Line);
}

