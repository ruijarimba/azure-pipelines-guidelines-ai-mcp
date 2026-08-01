using System.Text.Json.Serialization;

namespace AzurePipelines.Guidelines.Mcp.Resources;

internal sealed record GuidelineDetailDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("severity")] string Severity,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("rationale")] string? Rationale,
    [property: JsonPropertyName("tags")] string[]? Tags,
    [property: JsonPropertyName("detectionHints")] DetectionHintDto[]? DetectionHints,
    [property: JsonPropertyName("fix")] FixDto? Fix,
    [property: JsonPropertyName("references")] string[]? References);
