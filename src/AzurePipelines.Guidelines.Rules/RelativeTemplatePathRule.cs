using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules;

/// <summary>
/// ADOG-GENERAL-001 (consider): Detects template references that use a relative path
/// (e.g. <c>template: steps/build.yml</c>) instead of an absolute path starting with
/// <c>/</c> (e.g. <c>template: /templates/steps/build.yml</c>).
/// </summary>
internal sealed partial class RelativeTemplatePathRule : IGuidelineRule
{
    // Matches: template: <value> where:
    //   - value does NOT start with / (absolute path)
    //   - value does NOT contain @ (cross-repo reference like path.yml@alias)
    //   - value ends with .yml or .yaml
    [GeneratedRegex(
        @"template:\s+(?!/)[^@\n]+\.ya?ml(?!\s*@)",
        RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex RelativeTemplatePattern();

    private static readonly GuidelineId _id = new("ADOG-GENERAL-001");

    /// <inheritdoc/>
    public GuidelineId GuidelineId => _id;

    /// <inheritdoc/>
    public async IAsyncEnumerable<Diagnostic> EvaluateAsync(
        PipelineDocument document,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        await Task.CompletedTask; // satisfy async interface; work is synchronous

        foreach (Match match in RelativeTemplatePattern().Matches(document.RawContent))
        {
            cancellationToken.ThrowIfCancellationRequested();

            int line = RuleHelpers.GetLineNumber(document.RawContent, match.Index);

            yield return new Diagnostic(
                _id,
                DiagnosticSeverity.Info,
                $"Template reference '{match.Value.Trim()}' uses a relative path. " +
                "Consider using an absolute path (starting with '/') for clarity.",
                document.FilePath,
                line,
                Column: null);
        }
    }
}
