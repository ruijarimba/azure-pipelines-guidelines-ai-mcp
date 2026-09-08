using System.Text.Json.Serialization;

namespace AzurePipelines.Guidelines.Mcp.Resources;

internal sealed record ErrorResponseDto(
    [property: JsonPropertyName("error")] string Error);
