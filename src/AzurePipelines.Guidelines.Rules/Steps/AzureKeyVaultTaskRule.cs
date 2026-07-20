using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules.Steps;

/// <summary>
/// ADOG-STEPS-011 (do-not): Detects use of the <c>AzureKeyVault@N</c> task.
/// This task converts Key Vault secrets into pipeline variables and tightly couples
/// job steps. Use a variable group linked to Key Vault, referenced from a variables
/// template, with explicit step parameters instead.
/// </summary>
[RuleMetadata("ADOG-STEPS-011", "https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/donot-use-azurekeyvault-task.md")]
internal sealed partial class AzureKeyVaultTaskRule : IGuidelineRule
{
    [GeneratedRegex(
        @"task:\s*AzureKeyVault@\d+",
        RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex AzureKeyVaultPattern();

    private static readonly GuidelineId _id = new("ADOG-STEPS-011");

    /// <inheritdoc/>
    public GuidelineId GuidelineId => _id;

    /// <inheritdoc/>
    public async IAsyncEnumerable<Diagnostic> EvaluateAsync(
        PipelineDocument document,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        foreach (Match match in AzureKeyVaultPattern().Matches(document.RawContent))
        {
            cancellationToken.ThrowIfCancellationRequested();

            int line = RuleHelpers.GetLineNumber(document.RawContent, match.Index);

            yield return new Diagnostic(
                _id,
                DiagnosticSeverity.Error,
                "AzureKeyVault task detected. This task converts Key Vault secrets into " +
                "pipeline variables and tightly couples job steps. Use a variable group " +
                "linked to Key Vault, referenced from a variables template, with explicit " +
                "step parameters instead.",
                document.FilePath,
                line,
                Column: null);
        }
    }
}
