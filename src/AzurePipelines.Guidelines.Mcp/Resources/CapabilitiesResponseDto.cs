using System.Text.Json.Serialization;

namespace AzurePipelines.Guidelines.Mcp.Resources;

internal sealed record CapabilitiesResponseDto(
    [property: JsonPropertyName("server")] string Server,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("websiteUrl")] string WebsiteUrl,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("catalogueVersion")] string CatalogueVersion,
    [property: JsonPropertyName("transports")] string[] Transports,
    [property: JsonPropertyName("tools")] McpCapabilityDescriptorDto[] Tools,
    [property: JsonPropertyName("resources")] McpCapabilityDescriptorDto[] Resources,
    [property: JsonPropertyName("prompts")] McpCapabilityDescriptorDto[] Prompts,
    [property: JsonPropertyName("supports")] CapabilitiesSupportDto Supports);
