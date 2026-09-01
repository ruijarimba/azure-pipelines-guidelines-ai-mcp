using System.Runtime.CompilerServices;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules;

/// <summary>
/// ADOG-JOBS-003 (consider): Detects variables declared at the pipeline root while the
/// pipeline also defines jobs. Moving those values to job scope makes each job's inputs
/// more explicit and can reduce accidental reuse across unrelated jobs.
/// </summary>
[RuleMetadata("ADOG-JOBS-003", "https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/jobs/consider-job-variables.md")]
internal sealed class JobLevelVariableRule : IGuidelineRule
{
    private static readonly GuidelineId _id = new("ADOG-JOBS-003");

    /// <inheritdoc/>
    public GuidelineId GuidelineId => _id;

    /// <inheritdoc/>
    public async IAsyncEnumerable<Diagnostic> EvaluateAsync(
        PipelineDocument document,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (!document.AllJobs.Any())
        {
            yield break;
        }

        foreach (VariableNode variable in document.Variables)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (variable.Group is not null || variable.Value is null)
            {
                continue;
            }

            string variableName = RuleHelpers.SanitizeForDiagnostic(variable.Name ?? "(unnamed)");

            yield return new Diagnostic(
                _id,
                DiagnosticSeverity.Info,
                $"Variable '{variableName}' is declared at the pipeline root. " +
                "Consider moving it to job scope when it is only relevant to a specific job.",
                document.FilePath,
                Line: null,
                Column: null);
        }
    }
}
