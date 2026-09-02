namespace AzurePipelines.Guidelines.Core;

/// <summary>
/// Analyses a single <see cref="PipelineDocument"/> against the known guideline
/// definitions and returns the resulting findings.
/// </summary>
public interface IPipelineAnalyser
{
    /// <summary>
    /// Asynchronously analyses the given <paramref name="document"/> and returns
    /// an <see cref="AnalysisResult"/> containing any detected violations.
    /// </summary>
    /// <param name="document">The pipeline document to analyse.</param>
    /// <param name="options">Options that control which guidelines to evaluate.</param>
    /// <param name="cancellationToken">Token that can cancel the operation.</param>
    public Task<AnalysisResult> AnalyseAsync(
        PipelineDocument document,
        AnalysisOptions? options = null,
        CancellationToken cancellationToken = default);
}
