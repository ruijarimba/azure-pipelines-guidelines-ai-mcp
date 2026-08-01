using System.Text.Json.Serialization;

namespace AzurePipelines.Guidelines.Mcp.Tools;

internal sealed record GuidelineSummaryDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("severity")] string Severity);
