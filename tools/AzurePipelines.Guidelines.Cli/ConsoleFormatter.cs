using System.Text;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Cli;

/// <summary>
/// Formats an <see cref="AnalysisResult"/> as human-readable console output.
/// Each diagnostic is emitted on one line:
/// <c>{severity}: {ruleId} — {message} ({filePath}:{line})</c>
/// </summary>
internal static class ConsoleFormatter
{
    internal static string Format(AnalysisResult result)
    {
        if (result.Diagnostics.Count == 0)
        {
            return $"No violations found in {result.Document.FilePath}.{Environment.NewLine}";
        }

        StringBuilder sb = new();

        foreach (Diagnostic d in result.Diagnostics)
        {
            string severity = SeverityLabel(d.Severity);
            string location = d.Line.HasValue
                ? $"{d.FilePath}:{d.Line}"
                : d.FilePath;

            sb.Append(severity);
            sb.Append(": ");
            sb.Append(d.GuidelineId.Value);
            sb.Append(" \u2014 ");
            sb.Append(d.Message);
            sb.Append(" (");
            sb.Append(location);
            sb.Append(')');
            sb.AppendLine();
        }

        sb.AppendLine();
        sb.Append(result.Diagnostics.Count);
        sb.Append(result.Diagnostics.Count == 1 ? " violation" : " violations");
        sb.Append(" found in ");
        sb.Append(result.Document.FilePath);
        sb.Append('.');
        sb.AppendLine();

        return sb.ToString();
    }

    // Lowercase label matching the JSON serialisation convention.
    private static string SeverityLabel(DiagnosticSeverity severity) =>
        severity switch
        {
            DiagnosticSeverity.Error   => "error",
            DiagnosticSeverity.Warning => "warning",
            _                          => "info",
        };
}
