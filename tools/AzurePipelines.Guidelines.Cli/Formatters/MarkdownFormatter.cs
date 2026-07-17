using System.Globalization;
using System.Text;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Cli.Formatters;

/// <summary>
/// Formats analysis results as Markdown with tables and links to guideline documentation.
/// Suitable for PR comments, documentation, and human-readable reports.
/// </summary>
internal sealed class MarkdownFormatter : IOutputFormatter
{
    private const string _guidelineDocsBaseUrl = "https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/data/guidelines.json";

    /// <inheritdoc/>
    public string FormatName => "markdown";

    /// <inheritdoc/>
    public string Format(IReadOnlyList<AnalysisResult> results, bool useColor = true)
    {
        ArgumentNullException.ThrowIfNull(results);

        if (results.Count == 0)
        {
            return "# Azure Pipelines Guidelines Analysis\n\nNo files analysed.\n";
        }

        StringBuilder sb = new();

        sb.AppendLine("# Azure Pipelines Guidelines Analysis");
        sb.AppendLine();

        WriteSummary(sb, results);
        sb.AppendLine();

        bool hasViolations = results.Any(r => r.Diagnostics.Count > 0);
        if (hasViolations)
        {
            sb.AppendLine("## Violations");
            sb.AppendLine();

            foreach (AnalysisResult result in results)
            {
                if (result.Diagnostics.Count > 0)
                {
                    WriteFileViolations(sb, result);
                }
            }
        }
        else
        {
            sb.AppendLine("## ✅ No violations found!");
            sb.AppendLine();
            sb.AppendLine("All analysed files comply with the Azure Pipelines guidelines.");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static void WriteSummary(StringBuilder sb, IReadOnlyList<AnalysisResult> results)
    {
        int filesScanned = results.Count;
        int totalViolations = 0;
        int errors = 0;
        int warnings = 0;
        int info = 0;

        foreach (AnalysisResult result in results)
        {
            totalViolations += result.Diagnostics.Count;

            foreach (Diagnostic diagnostic in result.Diagnostics)
            {
                switch (diagnostic.Severity)
                {
                    case DiagnosticSeverity.Error:
                        errors++;
                        break;
                    case DiagnosticSeverity.Warning:
                        warnings++;
                        break;
                    case DiagnosticSeverity.Info:
                        info++;
                        break;
                }
            }
        }

        int cleanFiles = results.Count(r => r.Diagnostics.Count == 0);

        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine("| Metric | Count |");
        sb.AppendLine("|--------|-------|");
        sb.AppendLine(CultureInfo.InvariantCulture, $"| Files scanned | {filesScanned} |");
        sb.AppendLine(CultureInfo.InvariantCulture, $"| Total violations | {totalViolations} |");

        if (cleanFiles > 0)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"| Clean files | {cleanFiles} |");
        }

        if (errors > 0)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"| ❌ Errors | {errors} |");
        }

        if (warnings > 0)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"| ⚠️ Warnings | {warnings} |");
        }

        if (info > 0)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"| ℹ️ Info | {info} |");
        }
    }

    private static void WriteFileViolations(StringBuilder sb, AnalysisResult result)
    {
        sb.AppendLine(CultureInfo.InvariantCulture, $"### 📄 `{result.Document.FilePath}`");
        sb.AppendLine();
        sb.AppendLine("| Line | Severity | Rule | Message |");
        sb.AppendLine("|------|----------|------|---------|");

        foreach (Diagnostic diagnostic in result.Diagnostics)
        {
            string line = diagnostic.Line.HasValue
                ? diagnostic.Line.Value.ToString(CultureInfo.InvariantCulture)
                : "-";

            string severityIcon = diagnostic.Severity switch
            {
                DiagnosticSeverity.Error => "❌ Error",
                DiagnosticSeverity.Warning => "⚠️ Warning",
                DiagnosticSeverity.Info => "ℹ️ Info",
                _ => "Info",
            };

            string ruleLink = $"[{diagnostic.GuidelineId.Value}]({_guidelineDocsBaseUrl}#{diagnostic.GuidelineId.Value})";

            // Escape pipes so a diagnostic message cannot break the Markdown table layout.
            string escapedMessage = diagnostic.Message.Replace("|", "\\|", StringComparison.Ordinal);

            sb.AppendLine(CultureInfo.InvariantCulture, $"| {line} | {severityIcon} | {ruleLink} | {escapedMessage} |");
        }

        sb.AppendLine();
    }
}
