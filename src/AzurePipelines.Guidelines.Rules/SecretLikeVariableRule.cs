using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules;

/// <summary>
/// ADOG-VARIABLES-003 (do): Detects variable declarations whose name looks like a
/// secret (contains <c>password</c>, <c>secret</c>, <c>token</c>, <c>api_key</c>,
/// <c>apikey</c>, or <c>client_secret</c>) and have a non-empty inline value.
/// Secrets should never be stored as plain-text pipeline variables; use a secret
/// variable group or Azure Key Vault instead.
/// </summary>
internal sealed partial class SecretLikeVariableRule : IGuidelineRule
{
    // Matches two YAML variable styles that look like secrets with a plain-text value:
    //   Sequence block:   name: apiKey\n    value: plaintext
    //   Mapping:          password: plaintext
    [GeneratedRegex(
        @"(?i)(?:name:\s*\S*(?:password|secret|token|api[_\-]?key|client[_\-]?secret)\S*[^\n]*\n\s*value:\s*\S+|(?:password|secret|token|api[_\-]?key|client[_\-]?secret)\s*:\s*\S+)",
        RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex SecretLikePattern();

    private static readonly GuidelineId _id = new("ADOG-VARIABLES-003");

    /// <inheritdoc/>
    public GuidelineId GuidelineId => _id;

    /// <inheritdoc/>
    public async IAsyncEnumerable<Diagnostic> EvaluateAsync(
        PipelineDocument document,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        await Task.CompletedTask;

        foreach (Match match in SecretLikePattern().Matches(document.RawContent))
        {
            cancellationToken.ThrowIfCancellationRequested();

            int line = RuleHelpers.GetLineNumber(document.RawContent, match.Index);

            yield return new Diagnostic(
                _id,
                DiagnosticSeverity.Error,
                $"Variable name at line {line} looks like a secret. " +
                "Do not store secrets as plain-text pipeline variables. " +
                "Use a secret variable group or Azure Key Vault.",
                document.FilePath,
                line,
                Column: null);
        }
    }
}
