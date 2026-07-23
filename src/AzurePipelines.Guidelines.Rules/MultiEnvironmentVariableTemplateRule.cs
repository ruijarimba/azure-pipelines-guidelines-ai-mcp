using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules;

/// <summary>
/// ADOG-VARIABLES-006 (do-not): Detects variable templates that define values for multiple
/// environments in a single file, which makes the template harder to reuse and reason about.
/// Split per-environment values into separate variable templates instead.
/// </summary>
[RuleMetadata("ADOG-VARIABLES-006", "https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/variables/donot-mix-environments.md")]
internal sealed partial class MultiEnvironmentVariableTemplateRule : IGuidelineRule
{
    private static readonly GuidelineId _id = new("ADOG-VARIABLES-006");

    /// <inheritdoc/>
    public GuidelineId GuidelineId => _id;

    /// <inheritdoc/>
    public async IAsyncEnumerable<Diagnostic> EvaluateAsync(
        PipelineDocument document,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (!document.CommentFreeContent.Contains("variables:", StringComparison.OrdinalIgnoreCase))
        {
            yield break;
        }

        HashSet<string> environments = EnvironmentTokenPattern()
            .Matches(document.CommentFreeContent)
            .Select(match => match.Value.ToUpperInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (environments.Count >= 2)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return new Diagnostic(
                _id,
                DiagnosticSeverity.Error,
                "This variable template contains values for multiple environments. Split them into separate variable templates.",
                document.FilePath,
                Line: null,
                Column: null);
        }
    }

    [GeneratedRegex(@"\b(dev|test|qa|uat|prod|production|preprod|staging|stage|sandbox|preview|demo|int|integration)\b", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex EnvironmentTokenPattern();
}
