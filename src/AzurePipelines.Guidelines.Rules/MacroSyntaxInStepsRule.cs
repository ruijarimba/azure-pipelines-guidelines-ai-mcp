using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules;

/// <summary>
/// ADOG-STEPS-001 (avoid): Detects macro-syntax variable references (<c>$(VAR)</c>)
/// in pipeline YAML. Macro syntax expands at queue time and can fail silently when a
/// variable is undefined. Consider using runtime expressions (<c>$[variables.VAR]</c>)
/// instead.
/// </summary>
internal sealed partial class MacroSyntaxInStepsRule : IGuidelineRule
{
    [GeneratedRegex(
        @"\$\([A-Za-z0-9_.]+\)",
        RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex MacroPattern();

    private static readonly GuidelineId _id = new("ADOG-STEPS-001");

    /// <inheritdoc/>
    public GuidelineId GuidelineId => _id;

    /// <inheritdoc/>
    public async IAsyncEnumerable<Diagnostic> EvaluateAsync(
        PipelineDocument document,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        await Task.CompletedTask;

        foreach (Match match in MacroPattern().Matches(document.RawContent))
        {
            cancellationToken.ThrowIfCancellationRequested();

            int line = RuleHelpers.GetLineNumber(document.RawContent, match.Index);

            yield return new Diagnostic(
                _id,
                DiagnosticSeverity.Warning,
                $"Macro-syntax reference '{match.Value}' found. " +
                "Avoid $(VAR) macro syntax; prefer runtime expressions $[variables.VAR] " +
                "or template expressions ${{ variables.VAR }}.",
                document.FilePath,
                line,
                Column: null);
        }
    }
}
