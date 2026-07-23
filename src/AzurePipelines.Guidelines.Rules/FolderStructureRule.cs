using System.Runtime.CompilerServices;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules;

/// <summary>
/// ADOG-GENERAL-005 (do): Detects template references that use ad-hoc paths instead of a
/// consistent folder structure under shared roots such as <c>/templates/</c> or
/// <c>/pipelines/</c>.
/// </summary>
[RuleMetadata("ADOG-GENERAL-005", "https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/general/do-folder-structure.md")]
internal sealed class FolderStructureRule : IGuidelineRule
{
    private static readonly GuidelineId _id = new("ADOG-GENERAL-005");

    /// <inheritdoc/>
    public GuidelineId GuidelineId => _id;

    /// <inheritdoc/>
    public async IAsyncEnumerable<Diagnostic> EvaluateAsync(
        PipelineDocument document,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        string content = document.RawContent.Replace("\r\n", "\n", StringComparison.Ordinal);
        int lineNumber = 1;

        foreach (string line in content.Split('\n'))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (line.Contains("template:", StringComparison.OrdinalIgnoreCase))
            {
                string trimmed = line.Trim();
                int templateIndex = trimmed.IndexOf("template:", StringComparison.OrdinalIgnoreCase);
                if (templateIndex >= 0)
                {
                    string templateValue = trimmed[(templateIndex + "template:".Length)..].Trim();

                    if (templateValue.Length > 0 &&
                        templateValue[0] != '/' &&
                        !HasSharedRoot(templateValue))
                    {
                        yield return new Diagnostic(
                            _id,
                            DiagnosticSeverity.Error,
                            "Template paths should use a predictable shared root such as '/templates/' or '/pipelines/'.",
                            document.FilePath,
                            lineNumber,
                            Column: null);

                        yield break;
                    }
                }
            }

            lineNumber++;
        }
    }

    private static bool HasSharedRoot(string templateValue)
    {
        string value = templateValue.Trim();

        return value.Contains("/templates/", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("/pipelines/", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("/stages/", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("/jobs/", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("/steps/", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("/variables/", StringComparison.OrdinalIgnoreCase);
    }
}
