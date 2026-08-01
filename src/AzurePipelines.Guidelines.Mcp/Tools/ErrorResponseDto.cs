using System.Text.Json.Serialization;

namespace AzurePipelines.Guidelines.Mcp.Tools;

internal sealed record ErrorResponseDto(
    [property: JsonPropertyName("error")] string Error);
