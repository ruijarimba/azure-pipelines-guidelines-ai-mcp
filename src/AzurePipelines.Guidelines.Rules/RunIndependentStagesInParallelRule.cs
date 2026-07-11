using System.Runtime.CompilerServices;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules;

/// <summary>
/// ADOG-STAGES-002 (do): Detects pipelines that define multiple stages without any
/// explicit dependency information. Independent stages should be declared as parallel
/// stages so the pipeline can execute them concurrently.
/// </summary>
[RuleMetadata("ADOG-STAGES-002", "https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/stages/do-parallel-stages.md")]
internal sealed class RunIndependentStagesInParallelRule : IGuidelineRule
{
    private static readonly GuidelineId _id = new("ADOG-STAGES-002");

    /// <inheritdoc/>
    public GuidelineId GuidelineId => _id;

    /// <inheritdoc/>
    public async IAsyncEnumerable<Diagnostic> EvaluateAsync(
        PipelineDocument document,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.Stages.Count > 1 &&
            !document.RawContent.Contains("dependsOn", StringComparison.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return new Diagnostic(
                _id,
                DiagnosticSeverity.Error,
                "This pipeline declares multiple stages without explicit dependencies. Mark independent stages as parallel by declaring their dependency relationships clearly.",
                document.FilePath,
                Line: null,
                Column: null);
        }
    }
}
