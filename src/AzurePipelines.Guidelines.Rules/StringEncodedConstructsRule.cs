using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules;

/// <summary>
/// ADOG-GENERAL-002 (consider): Detects inline YAML values that use quoted JSON-style
/// list or map syntax instead of native YAML constructs. Prefer native lists, maps,
/// or multiline blocks so the file stays readable and schema-friendly.
/// </summary>
internal sealed partial class StringEncodedConstructsRule : IGuidelineRule
{
    private static readonly GuidelineId _id = new("ADOG-GENERAL-002");

    /// <inheritdoc/>
    public GuidelineId GuidelineId => _id;

    /// <inheritdoc/>
    public async IAsyncEnumerable<Diagnostic> EvaluateAsync(
        PipelineDocument document,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (SingleQuotedPattern().IsMatch(document.RawContent) ||
            DoubleQuotedPattern().IsMatch(document.RawContent))
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return new Diagnostic(
                _id,
                DiagnosticSeverity.Info,
                "String-encoded list or map values were found. Use native YAML lists, maps, or multiline blocks instead.",
                document.FilePath,
                Line: null,
                Column: null);
        }
    }

    [GeneratedRegex(
        "(?m)^\\s*(?:value|displayName|condition|script|name|template|path|file)\\s*:\\s*'[^'\\n]*(?:\\[|\\{)[^'\\n]*(?:\\]|\\})[^'\\n]*'",
        RegexOptions.CultureInvariant)]
    private static partial Regex SingleQuotedPattern();

    [GeneratedRegex(
        "(?m)^\\s*(?:value|displayName|condition|script|name|template|path|file)\\s*:\\s*\"[^\"\\n]*(?:\\[|\\{)[^\"\\n]*(?:\\]|\\})[^\"\\n]*\"",
        RegexOptions.CultureInvariant)]
    private static partial Regex DoubleQuotedPattern();
}
