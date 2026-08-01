using System.Text.Json.Serialization;

namespace AzurePipelines.Guidelines.Mcp.Resources;

internal sealed record GuidelineAutomationMetadataDto(
    [property: JsonPropertyName("guidelineId")] string GuidelineId,
    [property: JsonPropertyName("automationStatus")] string Status,
    [property: JsonPropertyName("automationReason")] string Reason);
