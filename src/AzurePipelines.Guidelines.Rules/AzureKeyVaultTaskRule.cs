using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules;

/// <summary>
/// ADOG-STEPS-011 (do-not): Detects use of the <c>AzureKeyVault@N</c> task.
/// Fetching secrets via a dedicated task exposes them as pipeline variables, which
/// can be leaked through logs. Use a managed identity and access Key Vault directly
/// from application code instead.
/// </summary>
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

        await Task.CompletedTask;

        foreach (Match match in AzureKeyVaultPattern().Matches(document.RawContent))
        {
            cancellationToken.ThrowIfCancellationRequested();

            int line = RuleHelpers.GetLineNumber(document.RawContent, match.Index);

            yield return new Diagnostic(
                _id,
                DiagnosticSeverity.Error,
                "AzureKeyVault task detected. Do not use the AzureKeyVault@N task to fetch " +
                "secrets into pipeline variables. Use a managed identity and access Key Vault " +
                "from application code instead.",
                document.FilePath,
                line,
                Column: null);
        }
    }
}
