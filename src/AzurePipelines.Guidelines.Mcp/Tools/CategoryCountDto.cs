using System.Text.Json.Serialization;

namespace AzurePipelines.Guidelines.Mcp.Tools;

internal sealed record CategoryCountDto(
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("count")] int Count);
