using System.Runtime.CompilerServices;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules.Jobs;

/// <summary>
/// ADOG-JOBS-002 (consider): Detects jobs that contain multiple logic steps,
/// excluding checkout steps.
/// Grouping related steps into a single steps template makes job logic easier to reuse
/// and reduces duplication across similar jobs.
/// </summary>
[RuleMetadata("ADOG-JOBS-002", "https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/jobs/consider-grouping-tasks.md")]
internal sealed class MultipleStepsTemplatesInJobRule : IGuidelineRule
{
    private static readonly GuidelineId _id = new("ADOG-JOBS-002");

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

            int logicStepCount = job.Steps.Count(step => !step.IsCheckout);
            if (logicStepCount > 1)
            {
                yield return new Diagnostic(
                    _id,
                    DiagnosticSeverity.Info,
                    "This job contains multiple logic steps. Consider consolidating the job logic into a single steps template.",
                    document.FilePath,
                    job.Line,
                    Column: null);
            }
        }
    }
}
