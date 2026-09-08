namespace AzurePipelines.Guidelines.Core;

/// <summary>
/// The result of analysing a single <see cref="PipelineDocument"/> against the loaded
/// set of <see cref="GuidelineDefinition"/> records.
/// </summary>
/// <param name="Document">The document that was analysed.</param>
/// <param name="Diagnostics">All findings produced for this document.</param>
public sealed record AnalysisResult(
    PipelineDocument Document,
    IReadOnlyList<Diagnostic> Diagnostics)
{
    /// <summary>
    /// Gets <see langword="true"/> when no findings were produced (or all were filtered out).
    /// </summary>
    public bool IsClean => Diagnostics.Count == 0;
}
