using System.Runtime.CompilerServices;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules;

/// <summary>
/// ADOG-JOBS-006 (do): Detects jobs that are missing a <c>timeoutInMinutes</c>
/// value. Without a timeout, a hung job can block a pipeline indefinitely.
/// </summary>
[RuleMetadata("ADOG-JOBS-006", "https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/jobs/do-job-timeouts.md")]
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
                string jobName = RuleHelpers.SanitizeForDiagnostic(job.Name ?? "(unnamed)");
                yield return new Diagnostic(
                    _id,
                    DiagnosticSeverity.Error,
                    $"Job '{jobName}' is missing 'timeoutInMinutes'. " +
                    "Set an explicit timeout to prevent indefinitely hung jobs.",
                    document.FilePath,
                    job.Line,
                    Column: null);
            }
        }
    }
}
