using System.Runtime.CompilerServices;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules;

/// <summary>
/// ADOG-JOBS-004 (do-not): Detects jobs that do not expose a validation-mode parameter.
/// A boolean parameter should allow the job to run without applying changes.
/// </summary>
[RuleMetadata("ADOG-JOBS-004", "https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/jobs/consider-validation-flag.md")]
internal sealed class ValidationModeJobParameterRule : IGuidelineRule
{
    private static readonly GuidelineId _id = new("ADOG-JOBS-004");

    /// <inheritdoc/>
    public GuidelineId GuidelineId => _id;

    /// <inheritdoc/>
    public async IAsyncEnumerable<Diagnostic> EvaluateAsync(
        PipelineDocument document,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        string content = document.CommentFreeContent.Replace("\r\n", "\n", StringComparison.Ordinal);
        bool hasJobDefinition = content.Contains("jobs:", StringComparison.OrdinalIgnoreCase)
            || content.Contains("job:", StringComparison.OrdinalIgnoreCase);
        bool hasValidationParameter = content.Contains("name: validationMode", StringComparison.OrdinalIgnoreCase)
            || content.Contains("name: validate", StringComparison.OrdinalIgnoreCase)
            || content.Contains("name: skipDeployment", StringComparison.OrdinalIgnoreCase)
            || content.Contains("validationMode", StringComparison.OrdinalIgnoreCase)
            || content.Contains("skipDeployment", StringComparison.OrdinalIgnoreCase);

        if (hasJobDefinition && !hasValidationParameter)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return new Diagnostic(
                _id,
                DiagnosticSeverity.Warning,
                "Add a boolean validation-mode parameter so the job can run without applying deployment changes.",
                document.FilePath,
                Line: null,
                Column: null);
        }
    }
}
