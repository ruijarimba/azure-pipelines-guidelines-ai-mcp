using System.Runtime.CompilerServices;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules;

/// <summary>
/// ADOG-VARIABLES-002 (consider): Detects variable templates that organize values by environment
/// without grouping them into dedicated folders or files.
/// </summary>
[RuleMetadata("ADOG-VARIABLES-002", "https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/variables/do-organize-variables.md")]
internal sealed class VariableTemplateOrganizationRule : IGuidelineRule
{
    private static readonly GuidelineId _id = new("ADOG-VARIABLES-002");

    /// <inheritdoc/>
    public GuidelineId GuidelineId => _id;

    /// <inheritdoc/>
    public async IAsyncEnumerable<Diagnostic> EvaluateAsync(
        PipelineDocument document,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        string content = document.RawContent.Replace("\r\n", "\n", StringComparison.Ordinal);
        bool hasVariableTemplates = content.Contains("template:", StringComparison.OrdinalIgnoreCase)
            && content.Contains("variables:", StringComparison.OrdinalIgnoreCase);
        bool isEnvironmentGrouped = content.Contains("dev", StringComparison.OrdinalIgnoreCase)
            || content.Contains("prod", StringComparison.OrdinalIgnoreCase)
            || content.Contains("test", StringComparison.OrdinalIgnoreCase)
            || content.Contains("qa", StringComparison.OrdinalIgnoreCase);

        if (hasVariableTemplates && isEnvironmentGrouped)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return new Diagnostic(
                _id,
                DiagnosticSeverity.Info,
                "Organize variable templates by environment or component in separate files or folders to make them easier to maintain.",
                document.FilePath,
                Line: null,
                Column: null);
        }
    }
}
