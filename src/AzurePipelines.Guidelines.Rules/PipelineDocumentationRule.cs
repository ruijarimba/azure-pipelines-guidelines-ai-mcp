using System.Runtime.CompilerServices;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules;

/// <summary>
/// ADOG-GENERAL-004 (do): Detects pipeline or template files that do not start with
/// a header comment describing the purpose and usage of the file.
/// </summary>
[RuleMetadata("ADOG-GENERAL-004", "https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/general/do-documentation.md")]
internal sealed class PipelineDocumentationRule : IGuidelineRule
{
    private static readonly GuidelineId _id = new("ADOG-GENERAL-004");

    /// <inheritdoc/>
    public GuidelineId GuidelineId => _id;

    /// <inheritdoc/>
    public async IAsyncEnumerable<Diagnostic> EvaluateAsync(
        PipelineDocument document,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        string[] lines = document.RawContent
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n');

        for (int index = 0; index < lines.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string line = lines[index];

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.TrimStart().StartsWith('#'))
            {
                yield break;
            }

            yield return new Diagnostic(
                _id,
                DiagnosticSeverity.Error,
                "Add a header comment describing the purpose, usage, and parameters of this pipeline or template.",
                document.FilePath,
                index + 1,
                Column: null);

            yield break;
        }
    }
}
