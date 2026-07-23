using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules.Steps;

/// <summary>
/// ADOG-STEPS-010 (do-not): Detects Azure Pipelines template or macro expressions
/// in step content. Expressions embedded in scripts make pipelines harder to read
/// and debug. Move values to the task boundary where appropriate.
/// </summary>
[RuleMetadata("ADOG-STEPS-010", "https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/donot-mix-syntax.md")]
internal sealed partial class LargeExpressionInStepsRule : IGuidelineRule
{
    // Matches: ${{ ... }} template expressions or $( ... ) macro expressions.
    // One diagnostic per file is reported (see below) to avoid flooding output.
    // Example:  "${{ variables.buildConfig }}"  or  "$(Build.SourceBranch)"
    [GeneratedRegex(
        @"\$\{\{.*?\}\}|\$\([^)]*\)",
        RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex ExpressionPattern();

    private static readonly GuidelineId _id = new("ADOG-STEPS-010");

    /// <inheritdoc/>
    public GuidelineId GuidelineId => _id;

    /// <inheritdoc/>
    public async IAsyncEnumerable<Diagnostic> EvaluateAsync(
        PipelineDocument document,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        // Report at most one diagnostic per file to avoid flooding the output
        // with every individual expression occurrence.
        Match first = ExpressionPattern().Match(document.RawContent);

        if (!first.Success)
        {
            yield break;
        }

        cancellationToken.ThrowIfCancellationRequested();

        int line = RuleHelpers.GetLineNumber(document.RawContent, first.Index);

        yield return new Diagnostic(
            _id,
            DiagnosticSeverity.Error,
            "Pipeline expressions ($(...) or ${{ ... }}) detected in step content. " +
            "Bind values at the task boundary where appropriate instead of embedding " +
            "them throughout script content.",
            document.FilePath,
            line,
            Column: null);
    }
}
