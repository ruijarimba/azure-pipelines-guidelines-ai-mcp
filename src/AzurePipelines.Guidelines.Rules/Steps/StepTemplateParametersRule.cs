using System.Runtime.CompilerServices;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules.Steps;

/// <summary>
/// ADOG-STEPS-007 (consider): Detects reusable step templates that rely on control settings
/// such as pool, condition, or dependsOn without exposing them as parameters.
/// </summary>
[RuleMetadata("ADOG-STEPS-007", "https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/do-extensible-steps.md")]
internal sealed class StepTemplateParametersRule : IGuidelineRule
{
    private static readonly GuidelineId _id = new("ADOG-STEPS-007");

    /// <inheritdoc/>
    public GuidelineId GuidelineId => _id;

    /// <inheritdoc/>
    public async IAsyncEnumerable<Diagnostic> EvaluateAsync(
        PipelineDocument document,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        string content = document.RawContent.Replace("\r\n", "\n", StringComparison.Ordinal);
        bool looksLikeTemplate = content.Contains("steps:", StringComparison.OrdinalIgnoreCase)
            && content.Contains("template:", StringComparison.OrdinalIgnoreCase);
        bool hasControlSetting = content.Contains("condition:", StringComparison.OrdinalIgnoreCase)
            || content.Contains("dependsOn:", StringComparison.OrdinalIgnoreCase)
            || content.Contains("pool:", StringComparison.OrdinalIgnoreCase);
        bool hasParametersBlock = content.Contains("parameters:", StringComparison.OrdinalIgnoreCase);

        if (looksLikeTemplate && hasControlSetting && !hasParametersBlock)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return new Diagnostic(
                _id,
                DiagnosticSeverity.Info,
                "Expose reusable step-template controls such as condition, dependsOn, or pool as parameters.",
                document.FilePath,
                Line: null,
                Column: null);
        }
    }
}
