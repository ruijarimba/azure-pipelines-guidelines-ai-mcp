using System.Runtime.CompilerServices;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules;

/// <summary>
/// ADOG-STEPS-009 (do): Detects step templates that accept parameters without any sign of
/// validation or fail-fast behavior.
/// </summary>
[RuleMetadata("ADOG-STEPS-009", "https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/do-validate-parameters.md")]
internal sealed class StepParameterValidationRule : IGuidelineRule
{
    private static readonly GuidelineId _id = new("ADOG-STEPS-009");

    /// <inheritdoc/>
    public GuidelineId GuidelineId => _id;

    /// <inheritdoc/>
    public async IAsyncEnumerable<Diagnostic> EvaluateAsync(
        PipelineDocument document,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        string content = document.RawContent.Replace("\r\n", "\n", StringComparison.Ordinal);
        bool hasParametersBlock = content.Contains("parameters:", StringComparison.OrdinalIgnoreCase);
        bool hasValidationLogic = content.Contains("if", StringComparison.OrdinalIgnoreCase)
            || content.Contains("throw", StringComparison.OrdinalIgnoreCase)
            || content.Contains("error", StringComparison.OrdinalIgnoreCase)
            || content.Contains("validate", StringComparison.OrdinalIgnoreCase);

        if (hasParametersBlock && !hasValidationLogic)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return new Diagnostic(
                _id,
                DiagnosticSeverity.Error,
                "Validate step parameters in templates and fail fast when input is invalid.",
                document.FilePath,
                Line: null,
                Column: null);
        }
    }
}
