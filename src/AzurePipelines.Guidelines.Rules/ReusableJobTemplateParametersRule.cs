using System.Runtime.CompilerServices;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules;

/// <summary>
/// ADOG-JOBS-005 (do): Detects reusable job templates that hard-code control settings such as
/// pool, dependsOn, or condition without exposing them through parameters.
/// </summary>
[RuleMetadata("ADOG-JOBS-005", "https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/jobs/do-extensible-jobs.md")]
internal sealed class ReusableJobTemplateParametersRule : IGuidelineRule
{
    private static readonly GuidelineId _id = new("ADOG-JOBS-005");

    /// <inheritdoc/>
    public GuidelineId GuidelineId => _id;

    /// <inheritdoc/>
    public async IAsyncEnumerable<Diagnostic> EvaluateAsync(
        PipelineDocument document,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        string content = document.CommentFreeContent.Replace("\r\n", "\n", StringComparison.Ordinal);
        bool hasJobTemplateStructure = content.Contains("jobs:", StringComparison.OrdinalIgnoreCase)
            || content.Contains("job:", StringComparison.OrdinalIgnoreCase)
            || content.Contains("template:", StringComparison.OrdinalIgnoreCase);
        bool hasControlSetting = content.Contains("pool:", StringComparison.OrdinalIgnoreCase)
            || content.Contains("dependsOn:", StringComparison.OrdinalIgnoreCase)
            || content.Contains("condition:", StringComparison.OrdinalIgnoreCase);
        bool hasParametersBlock = content.Contains("parameters:", StringComparison.OrdinalIgnoreCase);

        if (hasJobTemplateStructure && hasControlSetting && !hasParametersBlock)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return new Diagnostic(
                _id,
                DiagnosticSeverity.Warning,
                "Expose reusable job-template controls such as pool, dependsOn, or condition as parameters.",
                document.FilePath,
                Line: null,
                Column: null);
        }
    }
}
