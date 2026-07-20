using System.Runtime.CompilerServices;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules.Jobs;

/// <summary>
/// ADOG-JOBS-008 (do): Detects jobs that appear to combine multiple responsibilities.
/// Each job should focus on a single, well-defined responsibility.
/// </summary>
[RuleMetadata("ADOG-JOBS-008", "https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/jobs/do-single-responsibility.md")]
internal sealed class SingleResponsibilityJobRule : IGuidelineRule
{
    private static readonly GuidelineId _id = new("ADOG-JOBS-008");

    /// <inheritdoc/>
    public GuidelineId GuidelineId => _id;

    /// <inheritdoc/>
    public async IAsyncEnumerable<Diagnostic> EvaluateAsync(
        PipelineDocument document,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        string content = document.RawContent.Replace("\r\n", "\n", StringComparison.Ordinal);
        bool hasBuildLikeContent = content.Contains("build", StringComparison.OrdinalIgnoreCase)
            || content.Contains("test", StringComparison.OrdinalIgnoreCase)
            || content.Contains("deploy", StringComparison.OrdinalIgnoreCase);
        bool hasMultipleSignals = CountResponsibilitySignals(content) >= 2;

        if (hasBuildLikeContent && hasMultipleSignals)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return new Diagnostic(
                _id,
                DiagnosticSeverity.Error,
                "Split multi-purpose jobs so each job has a single responsibility and a clear purpose.",
                document.FilePath,
                Line: null,
                Column: null);
        }
    }

    private static int CountResponsibilitySignals(string content)
    {
        int count = 0;

        if (content.Contains("build", StringComparison.OrdinalIgnoreCase))
        {
            count++;
        }

        if (content.Contains("test", StringComparison.OrdinalIgnoreCase))
        {
            count++;
        }

        if (content.Contains("deploy", StringComparison.OrdinalIgnoreCase))
        {
            count++;
        }

        if (content.Contains("publish", StringComparison.OrdinalIgnoreCase))
        {
            count++;
        }

        return count;
    }
}
