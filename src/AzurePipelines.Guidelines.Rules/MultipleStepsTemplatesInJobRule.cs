using System.Runtime.CompilerServices;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules;

/// <summary>
/// ADOG-JOBS-002 (consider): Detects jobs that reference multiple steps templates.
/// Grouping related steps into a single steps template makes job logic easier to reuse
/// and reduces duplication across similar jobs.
/// </summary>
internal sealed class MultipleStepsTemplatesInJobRule : IGuidelineRule
{
    private static readonly GuidelineId _id = new("ADOG-JOBS-002");

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

            string trimmed = lines[index].Trim();
            if (!trimmed.StartsWith("- job:", StringComparison.Ordinal) &&
                !trimmed.StartsWith("job:", StringComparison.Ordinal))
            {
                continue;
            }

            int jobIndentation = lines[index].Length - lines[index].TrimStart().Length;
            int templateCount = 0;
            int? jobLine = index + 1;

            for (int nestedIndex = index + 1; nestedIndex < lines.Length; nestedIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string nestedLine = lines[nestedIndex];
                string nestedTrimmed = nestedLine.Trim();

                if (string.IsNullOrWhiteSpace(nestedTrimmed) || nestedTrimmed.StartsWith('#'))
                {
                    continue;
                }

                int nestedIndentation = nestedLine.Length - nestedLine.TrimStart().Length;
                if (nestedIndentation <= jobIndentation &&
                    (nestedTrimmed.StartsWith("- job:", StringComparison.Ordinal) ||
                     nestedTrimmed.StartsWith("job:", StringComparison.Ordinal)))
                {
                    break;
                }

                if (nestedTrimmed.StartsWith("- template:", StringComparison.Ordinal) ||
                    nestedTrimmed.StartsWith("template:", StringComparison.Ordinal))
                {
                    templateCount++;
                }
            }

            if (templateCount > 1 && jobLine is not null)
            {
                yield return new Diagnostic(
                    _id,
                    DiagnosticSeverity.Info,
                    "This job references multiple steps templates. Consider consolidating the shared steps into a single template.",
                    document.FilePath,
                    jobLine.Value,
                    Column: null);
            }
        }
    }
}
