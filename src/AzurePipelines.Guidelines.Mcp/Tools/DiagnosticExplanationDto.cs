using System.Text.Json.Serialization;

namespace AzurePipelines.Guidelines.Mcp.Tools;

internal sealed record DiagnosticExplanationDto(
    [property: JsonPropertyName("guideline")] GuidelineDetailDto Guideline,
    [property: JsonPropertyName("diagnostic")] DiagnosticContextDto? Diagnostic);
