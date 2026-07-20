namespace AzurePipelines.Guidelines.Core;

/// <summary>
/// The result of analysing a single <see cref="PipelineDocument"/> against the loaded
/// set of <see cref="GuidelineDefinition"/> records.
/// </summary>
/// <param name="Document">The document that was analysed.</param>
/// <param name="Diagnostics">All findings produced for this document.</param>
/// <param name="SchemaDiagnostics">Structural schema findings for this document.</param>
/// <param name="SkippedGuidelines">Rules that the analyser did not evaluate and why.</param>
public sealed record AnalysisResult(
    PipelineDocument Document,
    IReadOnlyList<Diagnostic> Diagnostics,
    IReadOnlyList<SchemaDiagnostic>? SchemaDiagnostics = null,
    IReadOnlyList<SkippedGuideline>? SkippedGuidelines = null)
{
    /// <summary>
    /// Gets the structural schema findings, or an empty collection when schema validation was not run.
    /// </summary>
    public IReadOnlyList<SchemaDiagnostic> StructuralDiagnostics => SchemaDiagnostics ?? [];

    /// <summary>
    /// Gets rules that were skipped because their local automation status did not allow evaluation.
    /// </summary>
    public IReadOnlyList<SkippedGuideline> SkippedRuleDetails => SkippedGuidelines ?? [];

    /// <summary>
    /// Gets <see langword="true"/> when no guideline or schema findings were produced.
    /// </summary>
    public bool IsClean => Diagnostics.Count == 0 && StructuralDiagnostics.Count == 0;
}
