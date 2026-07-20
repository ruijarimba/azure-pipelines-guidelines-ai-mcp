namespace AzurePipelines.Guidelines.Core;

/// <summary>
/// Options that control which guidelines are evaluated and how results are reported
/// during an analysis run.
/// </summary>
/// <param name="MinimumSeverity">
/// Discard diagnostics whose <see cref="DiagnosticSeverity"/> is lower than this value.
/// Defaults to <see cref="DiagnosticSeverity.Info"/> (include all findings).
/// </param>
/// <param name="IncludedCategories">
/// When non-empty, only guidelines belonging to one of these categories are evaluated.
/// An empty list means all categories are included.
/// </param>
/// <param name="IncludedGuidelineIds">
/// When non-empty, only guidelines whose ID is in this list are evaluated.
/// An empty list means all guidelines are evaluated (subject to
/// <see cref="IncludedCategories"/> filtering).
/// </param>
/// <param name="IncludedDiagnosticSeverities">
/// When non-empty, only diagnostics whose severity is in this list are retained.
/// An empty list means all severities are included (subject to
/// <see cref="MinimumSeverity"/> filtering).
/// </param>
/// <param name="IncludeHeuristics">
/// Gets whether rules that provide advisory, non-deterministic findings are evaluated.
/// Defaults to <see langword="false"/> to avoid noisy analysis results.
/// </param>
public sealed record AnalysisOptions(
    DiagnosticSeverity MinimumSeverity = DiagnosticSeverity.Info,
    IReadOnlyList<GuidelineCategory>? IncludedCategories = null,
    IReadOnlyList<GuidelineId>? IncludedGuidelineIds = null,
    IReadOnlyList<DiagnosticSeverity>? IncludedDiagnosticSeverities = null,
    bool IncludeHeuristics = false)
{
    /// <summary>Gets the default options: include deterministic findings.</summary>
    public static AnalysisOptions Default { get; } = new();
}
