using System.Text.Json;
using System.Text.Json.Serialization;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Cli;

/// <summary>
/// Formats an <see cref="AnalysisResult"/> as a JSON array of diagnostic objects.
/// </summary>
internal static class JsonFormatter
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    internal static string Format(AnalysisResult result)
    {
        DiagnosticDto[] dtos = new DiagnosticDto[result.Diagnostics.Count];
        for (int i = 0; i < result.Diagnostics.Count; i++)
        {
            Diagnostic d = result.Diagnostics[i];
            dtos[i] = new DiagnosticDto(
                d.GuidelineId.Value,
                SeverityLabel(d.Severity),
                d.Message,
                d.FilePath,
                d.Line);
        }

        return JsonSerializer.Serialize(dtos, _options);
    }

    // Lowercase label — avoids CA1308 via char-arithmetic on ASCII enum names.
    private static string SeverityLabel(DiagnosticSeverity value)
    {
        string name = value.ToString();
        return string.Create(name.Length, name, static (span, src) =>
        {
            for (int i = 0; i < src.Length; i++)
            {
                char c = src[i];
                span[i] = c is >= 'A' and <= 'Z' ? (char)(c + 32) : c;
            }
        });
    }

    private sealed record DiagnosticDto(
        [property: JsonPropertyName("ruleId")]   string RuleId,
        [property: JsonPropertyName("severity")] string Severity,
        [property: JsonPropertyName("message")]  string Message,
        [property: JsonPropertyName("filePath")] string FilePath,
        [property: JsonPropertyName("line")]     int? Line);
}
