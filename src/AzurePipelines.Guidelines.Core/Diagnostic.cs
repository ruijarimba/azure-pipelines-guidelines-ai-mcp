using System.Diagnostics;
using System.Globalization;

namespace AzurePipelines.Guidelines.Core;

/// <summary>
/// A single violation or finding produced by the analysis engine for a specific
/// location in a <see cref="PipelineDocument"/>.
/// </summary>
/// <param name="GuidelineId">The identifier of the violated guideline.</param>
/// <param name="Severity">The severity of this finding.</param>
/// <param name="Message">A human-readable message describing the finding.</param>
/// <param name="FilePath">The file path where the finding was detected.</param>
/// <param name="Line">The one-based line number, or <see langword="null"/> if unavailable.</param>
/// <param name="Column">The one-based column number, or <see langword="null"/> if unavailable.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record Diagnostic(
    GuidelineId GuidelineId,
    DiagnosticSeverity Severity,
    string Message,
    string FilePath,
    int? Line,
    int? Column)
{
    /// <inheritdoc/>
    public override string ToString() =>
        $"[{Severity}] {GuidelineId}: {Message} ({FilePath}:{Line?.ToString(CultureInfo.InvariantCulture) ?? "?"})";
}
