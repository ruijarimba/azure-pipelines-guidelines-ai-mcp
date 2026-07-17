using System.Text;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Cli.Formatters;

/// <summary>
/// Formats analysis results as human-readable console output with optional ANSI colors.
/// </summary>
internal sealed class ConsoleOutputFormatter : IOutputFormatter
{
    private const string _errorIcon = "❌";
    private const string _warningIcon = "⚠️ ";
    private const string _infoIcon = "ℹ️ ";

    // ANSI color codes keep terminal formatting separate from report content.
    private const string _redColor = "\x1b[31m";
    private const string _yellowColor = "\x1b[33m";
    private const string _cyanColor = "\x1b[36m";
    private const string _resetColor = "\x1b[0m";
    private const string _boldColor = "\x1b[1m";

    /// <inheritdoc/>
    public string FormatName => "console";

    /// <inheritdoc/>
    public string Format(IReadOnlyList<AnalysisResult> results, bool useColor = true)
    {
        ArgumentNullException.ThrowIfNull(results);

        if (results.Count == 0)
        {
            return $"No files analysed.{Environment.NewLine}";
        }

        StringBuilder sb = new();

        foreach (AnalysisResult result in results)
        {
            if (result.Diagnostics.Count == 0)
            {
                continue;
            }

            if (useColor)
            {
                sb.Append(_boldColor);
            }
            sb.AppendLine(result.Document.FilePath);
            if (useColor)
            {
                sb.Append(_resetColor);
            }

            foreach (Diagnostic diagnostic in result.Diagnostics)
            {
                FormatDiagnostic(sb, diagnostic, useColor);
            }

            sb.AppendLine();
        }

        FormatSummary(sb, results, useColor);

        return sb.ToString();
    }

    private static void FormatDiagnostic(StringBuilder sb, Diagnostic diagnostic, bool useColor)
    {
        string icon;
        string? color = null;

        switch (diagnostic.Severity)
        {
            case DiagnosticSeverity.Error:
                icon = _errorIcon;
                color = useColor ? _redColor : null;
                break;
            case DiagnosticSeverity.Warning:
                icon = _warningIcon;
                color = useColor ? _yellowColor : null;
                break;
            default:
                icon = _infoIcon;
                color = useColor ? _cyanColor : null;
                break;
        }

        if (color is not null)
        {
            sb.Append(color);
        }

        // Format: "  ❌ Error   ADOG-STEPS-001   Line 12   Macro syntax $(foo) in steps"
        sb.Append("  ");
        sb.Append(icon);
        sb.Append(' ');
        sb.Append(SeverityLabel(diagnostic.Severity).PadRight(8));
        sb.Append(diagnostic.GuidelineId.Value.PadRight(22));

        if (diagnostic.Line.HasValue)
        {
            sb.Append("Line ");
            sb.Append(diagnostic.Line.Value.ToString(System.Globalization.CultureInfo.InvariantCulture).PadRight(6));
        }
        else
        {
            sb.Append("".PadRight(11)); // Align with "Line NNN  "
        }

        sb.Append(diagnostic.Message);

        if (color is not null)
        {
            sb.Append(_resetColor);
        }

        sb.AppendLine();
    }

    private static void FormatSummary(StringBuilder sb, IReadOnlyList<AnalysisResult> results, bool useColor)
    {
        int totalDiagnostics = 0;
        int errorCount = 0;
        int warningCount = 0;
        int infoCount = 0;
        int cleanFiles = 0;

        foreach (AnalysisResult result in results)
        {
            totalDiagnostics += result.Diagnostics.Count;

            if (result.Diagnostics.Count == 0)
            {
                cleanFiles++;
            }

            foreach (Diagnostic diagnostic in result.Diagnostics)
            {
                switch (diagnostic.Severity)
                {
                    case DiagnosticSeverity.Error:
                        errorCount++;
                        break;
                    case DiagnosticSeverity.Warning:
                        warningCount++;
                        break;
                    default:
                        infoCount++;
                        break;
                }
            }
        }

        if (useColor)
        {
            sb.Append(_boldColor);
        }

        sb.AppendLine("Summary:");

        if (useColor)
        {
            sb.Append(_resetColor);
        }

        sb.Append("  Files scanned: ");
        sb.Append(results.Count.ToString(System.Globalization.CultureInfo.InvariantCulture));

        if (totalDiagnostics == 0)
        {
            sb.AppendLine("  ✅ No violations found");
        }
        else
        {
            sb.Append("  Violations: ");
            sb.Append(totalDiagnostics);
            sb.Append(" (");

            List<string> parts = [];
            if (errorCount > 0)
            {
                parts.Add($"{errorCount} error{(errorCount == 1 ? "" : "s")}");
            }
            if (warningCount > 0)
            {
                parts.Add($"{warningCount} warning{(warningCount == 1 ? "" : "s")}");
            }
            if (infoCount > 0)
            {
                parts.Add($"{infoCount} info");
            }

            sb.Append(string.Join(", ", parts));
            sb.AppendLine(")");
        }

        if (cleanFiles > 0)
        {
            sb.Append("  Clean files: ");
            sb.Append(cleanFiles.ToString(System.Globalization.CultureInfo.InvariantCulture));
            sb.AppendLine();
        }
    }

    private static string SeverityLabel(DiagnosticSeverity severity) =>
        severity switch
        {
            DiagnosticSeverity.Error => "Error",
            DiagnosticSeverity.Warning => "Warning",
            _ => "Info",
        };
}
