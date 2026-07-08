using System.Text;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Cli.Formatters;

/// <summary>
/// Formats analysis results as compact one-line-per-violation output.
/// Format: {file}:{line}:{column}: {severity}: [{ruleId}] {message}
/// </summary>
internal sealed class CompactFormatter : IOutputFormatter
{
    public string FormatName => "compact";

    public string Format(IReadOnlyList<AnalysisResult> results, bool useColor = true)
    {
        ArgumentNullException.ThrowIfNull(results);

        if (results.Count == 0)
        {
            return "No files analysed." + Environment.NewLine;
        }

        StringBuilder sb = new();

        foreach (AnalysisResult result in results)
        {
            foreach (Diagnostic diagnostic in result.Diagnostics)
            {
                FormatDiagnostic(sb, diagnostic, useColor);
            }
        }

        if (sb.Length == 0)
        {
            sb.AppendLine("No violations found.");
        }

        return sb.ToString();
    }

    private static void FormatDiagnostic(StringBuilder sb, Diagnostic diagnostic, bool useColor)
    {
        // File path
        sb.Append(diagnostic.FilePath);
        sb.Append(':');

        // Line number
        if (diagnostic.Line.HasValue)
        {
            sb.Append(diagnostic.Line.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        sb.Append(':');

        // Column number
        if (diagnostic.Column.HasValue)
        {
            sb.Append(diagnostic.Column.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        sb.Append(": ");

        // Severity (with optional color)
        string severity = diagnostic.Severity switch
        {
            DiagnosticSeverity.Error => "error",
            DiagnosticSeverity.Warning => "warning",
            DiagnosticSeverity.Info => "info",
            _ => "info",
        };

        if (useColor)
        {
            string color = diagnostic.Severity switch
            {
                DiagnosticSeverity.Error => "\x1b[31m",   // Red
                DiagnosticSeverity.Warning => "\x1b[33m", // Yellow
                _ => "\x1b[36m",                          // Cyan
            };
            sb.Append(color);
            sb.Append(severity);
            sb.Append("\x1b[0m"); // Reset
        }
        else
        {
            sb.Append(severity);
        }

        sb.Append(": ");

        // Rule ID
        sb.Append('[');
        sb.Append(diagnostic.GuidelineId.Value);
        sb.Append("] ");

        // Message
        sb.AppendLine(diagnostic.Message);
    }
}
