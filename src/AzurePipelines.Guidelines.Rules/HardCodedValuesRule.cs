using System.Runtime.CompilerServices;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules;

/// <summary>
/// ADOG-GENERAL-007 (do-not): Detects hard-coded literal values in YAML templates.
/// Prefer parameters or variables so the template can be reused across environments.
/// </summary>
internal sealed class HardCodedValuesRule : IGuidelineRule
{
    private static readonly GuidelineId _id = new("ADOG-GENERAL-007");

    /// <inheritdoc/>
    public GuidelineId GuidelineId => _id;

    /// <inheritdoc/>
    public async IAsyncEnumerable<Diagnostic> EvaluateAsync(
        PipelineDocument document,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        foreach (string line in document.RawContent.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!LooksLikeHardCodedValue(line))
            {
                continue;
            }

            yield return new Diagnostic(
                _id,
                DiagnosticSeverity.Error,
                "Replace hard-coded values with parameters or variables so the template can be reused safely.",
                document.FilePath,
                Line: null,
                Column: null);

            yield break;
        }
    }

    private static bool LooksLikeHardCodedValue(string line)
    {
        string trimmed = line.Trim();
        if (trimmed.Length == 0 || trimmed.StartsWith('#'))
        {
            return false;
        }

        if (trimmed.StartsWith("value:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("default:", StringComparison.OrdinalIgnoreCase))
        {
            int separatorIndex = trimmed.IndexOf(':', StringComparison.Ordinal);
            if (separatorIndex < 0)
            {
                return false;
            }

            string value = trimmed[(separatorIndex + 1)..].Trim();
            return value.Length > 0
                && !value.StartsWith('$')
                && !value.StartsWith("{{", StringComparison.Ordinal)
                && !value.StartsWith('\'')
                && !value.StartsWith('"')
                && !bool.TryParse(value, out _)
                && !int.TryParse(value, out _);
        }

        return false;
    }
}
