using System.Runtime.CompilerServices;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules;

/// <summary>
/// ADOG-VARIABLES-004 (do): Detects templates that hard-code configuration values instead of
/// sourcing them from variable templates or parameters.
/// </summary>
internal sealed class SeparateConfigurationRule : IGuidelineRule
{
    private static readonly GuidelineId _id = new("ADOG-VARIABLES-004");

    /// <inheritdoc/>
    public GuidelineId GuidelineId => _id;

    /// <inheritdoc/>
    public async IAsyncEnumerable<Diagnostic> EvaluateAsync(
        PipelineDocument document,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        string content = document.RawContent.Replace("\r\n", "\n", StringComparison.Ordinal);
        bool hasTemplateContent = content.Contains("template:", StringComparison.OrdinalIgnoreCase)
            || content.Contains("parameters:", StringComparison.OrdinalIgnoreCase);
        bool hasHardCodedConfig = content.Contains("environment:", StringComparison.OrdinalIgnoreCase)
            || content.Contains("region:", StringComparison.OrdinalIgnoreCase)
            || content.Contains("name:", StringComparison.OrdinalIgnoreCase)
            || content.Contains("url:", StringComparison.OrdinalIgnoreCase);
        bool hasVariableTemplate = content.Contains("variables:", StringComparison.OrdinalIgnoreCase)
            && content.Contains("template", StringComparison.OrdinalIgnoreCase);

        if (hasTemplateContent && hasHardCodedConfig && !hasVariableTemplate)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return new Diagnostic(
                _id,
                DiagnosticSeverity.Error,
                "Separate configuration from logic by sourcing values from variable templates instead of hard-coding them.",
                document.FilePath,
                Line: null,
                Column: null);
        }
    }
}
