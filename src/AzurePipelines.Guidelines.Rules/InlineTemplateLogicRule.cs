using System.Runtime.CompilerServices;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules;

/// <summary>
/// ADOG-GENERAL-006 (do-not): Detects pipelines or templates that define inline logic or
/// configuration directly instead of moving that content into reusable templates.
/// </summary>
[RuleMetadata("ADOG-GENERAL-006", "https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/general/do-templates-everywhere.md")]
internal sealed class InlineTemplateLogicRule : IGuidelineRule
{
    private static readonly GuidelineId _id = new("ADOG-GENERAL-006");

    /// <inheritdoc/>
    public GuidelineId GuidelineId => _id;

    /// <inheritdoc/>
    public async IAsyncEnumerable<Diagnostic> EvaluateAsync(
        PipelineDocument document,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        string content = document.RawContent.Replace("\r\n", "\n", StringComparison.Ordinal);
        bool hasInlineSteps = content.Contains("steps:", StringComparison.OrdinalIgnoreCase)
            && (content.Contains("script:", StringComparison.OrdinalIgnoreCase)
                || content.Contains("task:", StringComparison.OrdinalIgnoreCase));

        bool usesTemplates = content.Contains("template:", StringComparison.OrdinalIgnoreCase);

        if (hasInlineSteps && !usesTemplates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return new Diagnostic(
                _id,
                DiagnosticSeverity.Warning,
                "Define repeated logic in reusable templates and reference them instead of keeping inline steps in the pipeline.",
                document.FilePath,
                Line: null,
                Column: null);
        }
    }
}
