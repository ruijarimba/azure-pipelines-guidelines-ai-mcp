using System.Text.Json.Serialization;

namespace AzurePipelines.Guidelines.Mcp.Resources;

internal sealed record CatalogueVersionResponseDto(
    [property: JsonPropertyName("version")] string Version);
