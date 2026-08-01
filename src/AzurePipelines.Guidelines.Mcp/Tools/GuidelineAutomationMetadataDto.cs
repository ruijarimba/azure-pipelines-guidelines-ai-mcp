using System.Text.Json.Serialization;

namespace AzurePipelines.Guidelines.Mcp.Tools;

internal sealed record GuidelineAutomationMetadataDto(
    [property: JsonPropertyName("automationStatus")] string Status,
    [property: JsonPropertyName("automationReason")] string Reason);
