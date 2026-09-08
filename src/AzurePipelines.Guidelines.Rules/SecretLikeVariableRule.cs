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
[RuleMetadata("ADOG-VARIABLES-003", "https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/variables/do-sensitive-information.md")]
internal sealed partial class SecretLikeVariableRule : IGuidelineRule
{
    // Matches the YAML sequence block style where the variable name looks like a secret
    // and a plain-text value is set on the following line.
    // Matches:  name: apiKey\n    value: plaintext
    // Excludes: variables with no value line, or value set to a group/template reference
    // Example:  "  - name: apiToken\n    value: abc123"
    [GeneratedRegex(
        @"name:\s*\S*(?:password|secret|token|api[_\-]?key|client[_\-]?secret)\S*[^\n]*\n\s*value:\s*\S+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BlockStyleSecretPattern();

    // Matches the YAML mapping style where a key that looks like a secret has a plain-text value.
    // Matches:  password: plaintext
    // Excludes: keys that are not secret-like names
    // Example:  "  password: hunter2"
    [GeneratedRegex(
        @"(?:password|secret|token|api[_\-]?key|client[_\-]?secret)\s*:\s*\S+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MappingStyleSecretPattern();

    private static readonly GuidelineId _id = new("ADOG-VARIABLES-003");

    /// <inheritdoc/>
    public GuidelineId GuidelineId => _id;

    /// <inheritdoc/>
    public async IAsyncEnumerable<Diagnostic> EvaluateAsync(
        PipelineDocument document,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        foreach (Match match in BlockStyleSecretPattern().Matches(document.CommentFreeContent))
        {
            cancellationToken.ThrowIfCancellationRequested();

            int line = RuleHelpers.GetLineNumber(document.CommentFreeContent, match.Index);

            yield return CreateDiagnostic(document.FilePath, line);
        }

        foreach (Match match in MappingStyleSecretPattern().Matches(document.CommentFreeContent))
        {
            cancellationToken.ThrowIfCancellationRequested();

            int line = RuleHelpers.GetLineNumber(document.CommentFreeContent, match.Index);

            yield return CreateDiagnostic(document.FilePath, line);
        }
    }

    private static Diagnostic CreateDiagnostic(string filePath, int line) =>
        new(
            _id,
            DiagnosticSeverity.Error,
            "Variable name looks like a secret (password, token, or key). " +
            "Storing secrets as plain-text pipeline variables risks exposure in logs. " +
            "Use a secret variable group or Azure Key Vault instead.",
            filePath,
            line,
            Column: null);
}
