using System.Text.Json.Serialization;

namespace AzurePipelines.Guidelines.Mcp.Resources;

internal sealed record CapabilitiesSupportDto(
    [property: JsonPropertyName("automationMetadata")] bool AutomationMetadata,
    [property: JsonPropertyName("prompts")] bool Prompts);
