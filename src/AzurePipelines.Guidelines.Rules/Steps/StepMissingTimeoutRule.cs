using System.Runtime.CompilerServices;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules.Steps;

/// <summary>
/// ADOG-STEPS-006 (consider): Detects task steps (<c>task:</c>) that are missing a
/// <c>timeoutInMinutes</c> value. Task-level timeouts provide finer control than
/// job-level timeouts and make failure faster and more predictable.
/// </summary>
[RuleMetadata("ADOG-STEPS-006", "https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/consider-timeouts.md")]
internal sealed class StepMissingTimeoutRule : IGuidelineRule
{
    private static readonly GuidelineId _id = new("ADOG-STEPS-006");

    /// <inheritdoc/>
    public GuidelineId GuidelineId => _id;

    /// <inheritdoc/>
    public async IAsyncEnumerable<Diagnostic> EvaluateAsync(
        PipelineDocument document,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        foreach (StepNode step in document.AllSteps)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Only flag task: steps; script/checkout/template steps are exempt.
            if (step.Task is not null && step.TimeoutInMinutes is null)
            {
                string displayName = step.DisplayName ?? step.Task;

                yield return new Diagnostic(
                    _id,
                    DiagnosticSeverity.Info,
                    $"Task step '{displayName}' is missing 'timeoutInMinutes'. " +
                    "Consider setting a step-level timeout for faster failure detection.",
                    document.FilePath,
                    step.Line,
                    Column: null);
            }
        }
    }
}
