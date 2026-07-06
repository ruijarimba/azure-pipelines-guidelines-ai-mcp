using System.Runtime.CompilerServices;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules;

/// <summary>
/// ADOG-JOBS-006 (do): Detects jobs that are missing a <c>timeoutInMinutes</c>
/// value. Without a timeout, a hung job can block a pipeline indefinitely.
/// </summary>
internal sealed class JobMissingTimeoutRule : IGuidelineRule
{
    private static readonly GuidelineId _id = new("ADOG-JOBS-006");

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

            if (job.TimeoutInMinutes is null)
            {
                yield return new Diagnostic(
                    _id,
                    DiagnosticSeverity.Error,
                    $"Job '{job.Name ?? "(unnamed)"}' is missing 'timeoutInMinutes'. " +
                    "Set an explicit timeout to prevent indefinitely hung jobs.",
                    document.FilePath,
                    job.Line,
                    Column: null);
            }
        }
    }
}
