namespace AzurePipelines.Guidelines.Core;

/// <summary>
/// Evaluates a single parsed pipeline document against one guideline and returns
/// any violations found. Implementations live in the <c>Rules</c> project.
/// </summary>
public interface IGuidelineRule
{
    /// <summary>Gets the identifier of the guideline this rule enforces.</summary>
    public GuidelineId GuidelineId
    {
        get;
    }

    /// <summary>
    /// Evaluates the <paramref name="document"/> and yields any violations found.
    /// </summary>
    /// <param name="document">The pipeline document to evaluate.</param>
    /// <param name="cancellationToken">Token that can cancel the operation.</param>
    /// <returns>
    /// An async enumerable of <see cref="Diagnostic"/> instances representing
    /// detected violations. Returns an empty sequence when the document is compliant.
    /// </returns>
    public IAsyncEnumerable<Diagnostic> EvaluateAsync(
        PipelineDocument document,
        CancellationToken cancellationToken = default);
}
