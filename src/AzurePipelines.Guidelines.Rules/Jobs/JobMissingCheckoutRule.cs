using System.Runtime.CompilerServices;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules.Jobs;

/// <summary>
/// ADOG-JOBS-001 (consider): Detects jobs that have no explicit <c>checkout</c> step.
/// Add one or more checkout steps with the required repository value to make the job's
/// source checkout intent clear.
/// </summary>
[RuleMetadata("ADOG-JOBS-001", "https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/jobs/consider-explicit-checkout.md")]
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
