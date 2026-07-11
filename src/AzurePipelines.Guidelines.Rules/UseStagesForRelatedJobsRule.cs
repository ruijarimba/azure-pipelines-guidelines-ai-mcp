using System.Runtime.CompilerServices;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules;

/// <summary>
/// ADOG-STAGES-001 (consider): Detects pipelines that define multiple top-level jobs but no
/// explicit stages. Grouping related jobs into stages makes dependencies and execution order
/// clearer for both humans and automation.
/// </summary>
[RuleMetadata("ADOG-STAGES-001", "https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/stages/consider-grouping-jobs.md")]
internal sealed class UseStagesForRelatedJobsRule : IGuidelineRule
{
    private static readonly GuidelineId _id = new("ADOG-STAGES-001");

    /// <inheritdoc/>
    public GuidelineId GuidelineId => _id;

    /// <inheritdoc/>
    public async IAsyncEnumerable<Diagnostic> EvaluateAsync(
        PipelineDocument document,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.Jobs.Count > 1 && document.Stages.Count == 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return new Diagnostic(
                _id,
                DiagnosticSeverity.Info,
                "This pipeline defines multiple top-level jobs. Consider grouping related jobs into stages to make dependencies clearer.",
                document.FilePath,
                Line: null,
                Column: null);
        }
    }
}
