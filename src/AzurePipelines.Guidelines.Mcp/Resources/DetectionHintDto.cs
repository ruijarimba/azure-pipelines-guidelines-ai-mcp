using System.Text.Json.Serialization;

namespace AzurePipelines.Guidelines.Mcp.Resources;

internal sealed record DetectionHintDto(
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("scope")] string Scope,
    [property: JsonPropertyName("expression")] string? Expression,
    [property: JsonPropertyName("description")] string Description);
