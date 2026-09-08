using System.Text.Json.Serialization;

namespace AzurePipelines.Guidelines.Mcp.Tools;

internal sealed record FixDto(
    [property: JsonPropertyName("summary")] string Summary,
    [property: JsonPropertyName("before")] string? Before,
    [property: JsonPropertyName("after")] string? After);
