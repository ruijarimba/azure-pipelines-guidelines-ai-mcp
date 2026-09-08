using System.Runtime.CompilerServices;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules;

/// <summary>
/// ADOG-PIPELINES-001 (consider): Detects pipelines that do not expose a validation-mode
/// switch via a parameter or condition. Making validation mode explicit helps teams run
/// the same pipeline without applying deployment changes.
/// </summary>
[RuleMetadata("ADOG-PIPELINES-001", "https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/pipelines/consider-validation.md")]
internal sealed class PipelineValidationModeRule : IGuidelineRule
{
    private static readonly GuidelineId _id = new("ADOG-PIPELINES-001");

    /// <inheritdoc/>
    public GuidelineId GuidelineId => _id;

    /// <inheritdoc/>
    public async IAsyncEnumerable<Diagnostic> EvaluateAsync(
        PipelineDocument document,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        string content = document.CommentFreeContent;
        bool hasValidationHint = content.Contains("validation", StringComparison.OrdinalIgnoreCase)
            || content.Contains("validate", StringComparison.OrdinalIgnoreCase)
            || content.Contains("skipDeployment", StringComparison.OrdinalIgnoreCase)
            || content.Contains("runValidation", StringComparison.OrdinalIgnoreCase)
            || content.Contains("validationMode", StringComparison.OrdinalIgnoreCase);

        if (!hasValidationHint)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return new Diagnostic(
                _id,
                DiagnosticSeverity.Info,
                "Add a validation-mode parameter or condition so the pipeline can run without applying deployment changes.",
                document.FilePath,
                Line: null,
                Column: null);
        }
    }
}
