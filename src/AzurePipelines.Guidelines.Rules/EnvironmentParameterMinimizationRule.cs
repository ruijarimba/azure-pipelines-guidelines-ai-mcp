using System.Runtime.CompilerServices;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules;

/// <summary>
/// ADOG-JOBS-007 (consider): Detects reusable job templates that expose many environment-related
/// parameters instead of sourcing environment values from variable templates.
/// </summary>
internal sealed class EnvironmentParameterMinimizationRule : IGuidelineRule
{
    private static readonly GuidelineId _id = new("ADOG-JOBS-007");

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
        bool hasEnvironmentParameters = content.Contains("environment", StringComparison.OrdinalIgnoreCase)
            || content.Contains("env", StringComparison.OrdinalIgnoreCase)
            || content.Contains("stage", StringComparison.OrdinalIgnoreCase)
            || content.Contains("region", StringComparison.OrdinalIgnoreCase);
        bool hasVariableTemplates = content.Contains("variables:", StringComparison.OrdinalIgnoreCase)
            && content.Contains("template", StringComparison.OrdinalIgnoreCase);

        if (hasParametersBlock && hasEnvironmentParameters && !hasVariableTemplates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return new Diagnostic(
                _id,
                DiagnosticSeverity.Info,
                "Minimize environment-related parameters in reusable job templates and source values from variable templates instead.",
                document.FilePath,
                Line: null,
                Column: null);
        }
    }
}
