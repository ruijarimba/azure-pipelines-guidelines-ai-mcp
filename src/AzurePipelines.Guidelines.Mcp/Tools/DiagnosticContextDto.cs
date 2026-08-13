using System.Text.Json.Serialization;

namespace AzurePipelines.Guidelines.Mcp.Tools;

internal sealed record DiagnosticContextDto(
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("filePath")] string? FilePath,
    [property: JsonPropertyName("line")] int? Line,
    [property: JsonPropertyName("column")] int? Column);
