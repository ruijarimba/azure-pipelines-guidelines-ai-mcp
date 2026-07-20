using System.Runtime.CompilerServices;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules.Steps;

/// <summary>
/// ADOG-STEPS-005 (consider): Detects step blocks that do not declare a retry count when
/// the step is intended to tolerate transient failures. Adding a retry count makes the
/// behavior explicit and reduces brittle pipelines.
/// </summary>
[RuleMetadata("ADOG-STEPS-005", "https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/consider-retries.md")]
internal sealed class StepRetryRule : IGuidelineRule
{
    private static readonly GuidelineId _id = new("ADOG-STEPS-005");

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
            if (!TryParseStepStart(line, out int indentation))
            {
                continue;
            }

            int blockEnd = FindBlockEnd(lines, index + 1, indentation);
            bool hasRetryConfig = false;

            for (int nestedIndex = index + 1; nestedIndex < blockEnd; nestedIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (lines[nestedIndex].Contains("retryCountOnTaskFailure", StringComparison.OrdinalIgnoreCase))
                {
                    hasRetryConfig = true;
                    break;
                }
            }

            if (!hasRetryConfig)
            {
                yield return new Diagnostic(
                    _id,
                    DiagnosticSeverity.Info,
                    "Add a retry count for this step when transient failures should be retried automatically.",
                    document.FilePath,
                    index + 1,
                    Column: null);
            }
        }
    }

    private static bool TryParseStepStart(string line, out int indentation)
    {
        indentation = 0;
        string trimmed = line.Trim();
        if (trimmed.Length == 0 || trimmed.StartsWith('#'))
        {
            return false;
        }

        int leadingSpaces = line.Length - line.TrimStart().Length;
        indentation = leadingSpaces;

        return trimmed.StartsWith("- ", StringComparison.Ordinal)
            && (trimmed.Contains("script:", StringComparison.Ordinal)
                || trimmed.Contains("task:", StringComparison.Ordinal)
                || trimmed.Contains("bash:", StringComparison.Ordinal)
                || trimmed.Contains("pwsh:", StringComparison.Ordinal)
                || trimmed.Contains("powershell:", StringComparison.Ordinal));
    }

    private static int FindBlockEnd(string[] lines, int startIndex, int indentation)
    {
        for (int index = startIndex; index < lines.Length; index++)
        {
            string line = lines[index];
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
            {
                continue;
            }

            int currentIndentation = line.Length - line.TrimStart().Length;
            if (currentIndentation <= indentation)
            {
                return index;
            }
        }

        return lines.Length;
    }
}
