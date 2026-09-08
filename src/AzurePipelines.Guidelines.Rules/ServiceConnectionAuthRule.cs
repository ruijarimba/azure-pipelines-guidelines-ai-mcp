using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules;

/// <summary>
/// ADOG-STEPS-008 (do): Detects steps that appear to access an external service but do not
/// use a service connection or equivalent authentication input. Using explicit service
/// connections makes the dependency and credentials scope clear.
/// </summary>
[RuleMetadata("ADOG-STEPS-008", "https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/do-use-service-connections.md")]
internal sealed partial class ServiceConnectionAuthRule : IGuidelineRule
{
    private static readonly GuidelineId _id = new("ADOG-STEPS-008");

    /// <inheritdoc/>
    public GuidelineId GuidelineId => _id;

    /// <inheritdoc/>
    public async IAsyncEnumerable<Diagnostic> EvaluateAsync(
        PipelineDocument document,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        string[] lines = document.CommentFreeContent
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n');

        for (int index = 0; index < lines.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string line = lines[index];
            if (!LooksLikeExternalStep(line))
            {
                continue;
            }

            int stepIndentation = line.Length - line.TrimStart().Length;
            int blockEnd = FindBlockEnd(lines, index + 1, stepIndentation);
            string block = string.Join("\n", lines[index..blockEnd]);

            if (!HasServiceConnectionReference(block))
            {
                yield return new Diagnostic(
                    _id,
                    DiagnosticSeverity.Error,
                    "This step appears to target an external service but does not reference a service connection. Use an explicit service connection for authentication.",
                    document.FilePath,
                    index + 1,
                    Column: null);
            }
        }
    }

    private static bool LooksLikeExternalStep(string line)
    {
        string trimmed = line.Trim();
        if (trimmed.Length == 0 || trimmed.StartsWith('#'))
        {
            return false;
        }

        return trimmed.StartsWith("- script:", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("- task:", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("script:", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("task:", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("- pwsh:", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("- bash:", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("pwsh:", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("bash:", StringComparison.OrdinalIgnoreCase)
            || ExternalCommandPattern().IsMatch(trimmed);
    }

    private static bool HasServiceConnectionReference(string block)
        => ServiceConnectionPattern().IsMatch(block);

    [GeneratedRegex(
        @"connectedServiceName|serviceConnection|azureSubscription|azure_subscription|containerRegistry|githubToken",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex ServiceConnectionPattern();

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

    [GeneratedRegex(@"\b(?:az|docker|kubectl|helm|gh|terraform|azure|github)\b", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex ExternalCommandPattern();
}
