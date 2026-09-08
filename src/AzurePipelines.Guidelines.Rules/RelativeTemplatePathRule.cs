using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules;

/// <summary>
/// ADOG-GENERAL-001 (consider): Detects template references that use a relative path
/// (e.g. <c>template: steps/build.yml</c>) instead of an absolute path starting with
/// <c>/</c> (e.g. <c>template: /templates/steps/build.yml</c>).
/// </summary>
[RuleMetadata("ADOG-GENERAL-001", "https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/general/consider-absolute-paths.md")]
internal sealed partial class RelativeTemplatePathRule : IGuidelineRule
{
    // Matches:  template: <value> where value ends with .yml or .yaml
    // Requires: at least one space after the colon (so \s+ prevents the lookahead
    //           from inspecting the colon character itself).
    // Excludes: absolute paths (value starts with /) via negative lookahead (?!/)
    // Excludes: cross-repo references (value contains @alias) via trailing (?!\s*@)
    // Example:  "  - template: steps/build.yml"
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

        foreach (Match match in RelativeTemplatePattern().Matches(document.CommentFreeContent))
        {
            cancellationToken.ThrowIfCancellationRequested();

            int line = RuleHelpers.GetLineNumber(document.CommentFreeContent, match.Index);
            string templateReference = RuleHelpers.SanitizeForDiagnostic(match.Value.Trim());

            yield return new Diagnostic(
                _id,
                DiagnosticSeverity.Info,
                $"Template reference '{templateReference}' uses a relative path. " +
                "Consider using an absolute path (starting with '/') for clarity.",
                document.FilePath,
                line,
                Column: null);
        }
    }
}
