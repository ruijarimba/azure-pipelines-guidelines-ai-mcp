using System.Text.Json.Serialization;

namespace AzurePipelines.Guidelines.Mcp.Resources;

internal sealed record CapabilitiesResponseDto(
    [property: JsonPropertyName("server")] string Server,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("catalogueVersion")] string CatalogueVersion,
    [property: JsonPropertyName("transports")] string[] Transports,
    [property: JsonPropertyName("tools")] string[] Tools,
    [property: JsonPropertyName("resources")] string[] Resources,
    [property: JsonPropertyName("prompts")] string[] Prompts,
    [property: JsonPropertyName("supports")] CapabilitiesSupportDto Supports);
