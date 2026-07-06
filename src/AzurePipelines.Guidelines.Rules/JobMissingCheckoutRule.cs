using System.Runtime.CompilerServices;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules;

/// <summary>
/// ADOG-JOBS-001 (consider): Detects jobs that have no <c>checkout</c> step.
/// Most CI jobs need source code; omitting an explicit checkout may rely on implicit
/// behaviour that can change. Add <c>- checkout: self</c> (or <c>checkout: none</c>
/// when source is intentionally not needed) to make intent clear.
/// </summary>
internal sealed class JobMissingCheckoutRule : IGuidelineRule
{
    private static readonly GuidelineId _id = new("ADOG-JOBS-001");

    /// <inheritdoc/>
    public GuidelineId GuidelineId => _id;

    /// <inheritdoc/>
    public async IAsyncEnumerable<Diagnostic> EvaluateAsync(
        PipelineDocument document,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        await Task.CompletedTask;

        foreach (JobNode job in document.AllJobs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            bool hasCheckout = job.Steps.Any(s => s.IsCheckout);

            if (!hasCheckout)
            {
                yield return new Diagnostic(
                    _id,
                    DiagnosticSeverity.Info,
                    $"Job '{job.Name ?? "(unnamed)"}' has no checkout step. " +
                    "Consider adding '- checkout: self' or '- checkout: none' to be explicit.",
                    document.FilePath,
                    job.Line,
                    Column: null);
            }
        }
    }
}
