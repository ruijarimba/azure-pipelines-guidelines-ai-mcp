using System.Text;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Cli;

/// <summary>
/// Formats analysis results and guideline definitions as human-readable console output.
/// </summary>
internal static class ConsoleFormatter
{
    // ── Guidelines ────────────────────────────────────────────────────────────

    /// <summary>
    /// Formats a list of guideline summaries: one line per guideline —
    /// <c>{id}  {severity}  {title}</c>
    /// </summary>
    internal static string FormatGuidelineList(IReadOnlyList<GuidelineDefinition> guidelines)
    {
        if (guidelines.Count == 0)
        {
            return $"No guidelines found.{Environment.NewLine}";
        }

        StringBuilder sb = new();

        foreach (GuidelineDefinition g in guidelines)
        {
            sb.Append(g.Id.Value.PadRight(22));
            sb.Append(GuidelineSeverityLabel(g.Severity).PadRight(10));
            sb.AppendLine(g.Title);
        }

        sb.AppendLine();
        sb.Append(guidelines.Count);
        sb.Append(guidelines.Count == 1 ? " guideline" : " guidelines");
        sb.AppendLine(".");

        return sb.ToString();
    }

    /// <summary>
    /// Formats the full detail of a single guideline definition.
    /// </summary>
    internal static string FormatGuidelineDetail(GuidelineDefinition g)
    {
        StringBuilder sb = new();

        sb.Append(g.Id.Value);
        sb.Append(" — ");
        sb.AppendLine(g.Title);
        sb.AppendLine(new string('-', 60));

        sb.Append("Category : "); sb.AppendLine(g.Category.ToString().ToUpperInvariant());
        sb.Append("Severity : "); sb.AppendLine(GuidelineSeverityLabel(g.Severity));
        sb.AppendLine();

        sb.AppendLine("Description:");
        sb.AppendLine(g.Description);

        if (!string.IsNullOrWhiteSpace(g.Rationale))
        {
            sb.AppendLine();
            sb.AppendLine("Rationale:");
            sb.AppendLine(g.Rationale);
        }

        if (g.Fix is not null)
        {
            sb.AppendLine();
            sb.AppendLine("Fix:");
            sb.AppendLine(g.Fix.Summary);
        }

        if (g.References.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("References:");
            foreach (string ref_ in g.References)
            {
                sb.Append("  ");
                sb.AppendLine(ref_);
            }
        }

        return sb.ToString();
    }

    // Guideline severity uses GuidelineSeverity (do/avoid/consider), distinct from DiagnosticSeverity.
    private static string GuidelineSeverityLabel(GuidelineSeverity severity) =>
        severity switch
        {
            GuidelineSeverity.Do       => "do",
            GuidelineSeverity.DoNot    => "do-not",
            GuidelineSeverity.Avoid    => "avoid",
            GuidelineSeverity.Consider => "consider",
            _                          => EnumToLower(severity),
        };

    // Converts an enum value to lowercase ASCII — avoids CA1308 (ToLowerInvariant).
    private static string EnumToLower<T>(T value) where T : struct, Enum
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

    // ── Analysis results ──────────────────────────────────────────────────────

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

    internal static string Format(IReadOnlyList<AnalysisResult> results)
    {
        if (results.Count == 0)
        {
            return $"No files analysed.{Environment.NewLine}";
        }

        StringBuilder sb = new();
        foreach (AnalysisResult result in results)
        {
            sb.Append(Format(result));
        }

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
