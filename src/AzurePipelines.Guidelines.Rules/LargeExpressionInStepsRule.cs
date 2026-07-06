using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules;

/// <summary>
/// ADOG-STEPS-010 (do-not): Detects template or macro expressions used directly
/// inside step definitions. Complex inline expressions make pipelines hard to read
/// and debug. Move logic into parameters or variables instead.
/// </summary>
internal sealed partial class LargeExpressionInStepsRule : IGuidelineRule
{
    // Matches: ${{ ... }} template expressions or $( ... ) macro expressions.
    // This pattern intentionally matches both opening forms; the closing delimiters
    // are optional so that partial or multi-line expressions are also detected.
    // One diagnostic per file is reported (see below) to avoid flooding output.
    // Example:  "${{ variables.buildConfig }}"  or  "$(Build.SourceBranch)"
    [GeneratedRegex(
        @"(\$\{\{|\$\()[^)]*\}?\}?",
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
            "Inline expressions (${{ }} or $()) detected in step definitions. " +
            "Do not embed complex expressions directly in steps; " +
            "move values into parameters or variables for readability.",
            document.FilePath,
            line,
            Column: null);
    }
}
