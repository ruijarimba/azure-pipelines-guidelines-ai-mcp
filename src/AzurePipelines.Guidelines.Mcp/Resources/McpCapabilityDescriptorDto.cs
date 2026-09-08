using System.Text.Json.Serialization;

namespace AzurePipelines.Guidelines.Mcp.Resources;

internal sealed record McpCapabilityDescriptorDto(
    [property: JsonPropertyName("identifier")] string Identifier,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")] string Description);
