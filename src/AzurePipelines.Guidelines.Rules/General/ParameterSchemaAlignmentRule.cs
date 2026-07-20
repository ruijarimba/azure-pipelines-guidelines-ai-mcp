using System.Runtime.CompilerServices;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules.General;

/// <summary>
/// ADOG-GENERAL-003 (do): Detects parameters that map to common Azure Pipelines YAML fields
/// but use an incompatible type, which makes template reuse harder.
/// </summary>
[RuleMetadata("ADOG-GENERAL-003", "https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/general/consider-schema-compatible-types.md")]
internal sealed class ParameterSchemaAlignmentRule : IGuidelineRule
{
    private static readonly GuidelineId _id = new("ADOG-GENERAL-003");

    /// <inheritdoc/>
    public GuidelineId GuidelineId => _id;

    /// <inheritdoc/>
    public async IAsyncEnumerable<Diagnostic> EvaluateAsync(
        PipelineDocument document,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        string content = document.RawContent.Replace("\r\n", "\n", StringComparison.Ordinal);
        string[] lines = content.Split('\n');
        for (int index = 0; index < lines.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!TryGetParameterDeclaration(lines, index, out string? parameterName, out string? parameterType))
            {
                continue;
            }

            if (parameterName is null || parameterType is null)
            {
                continue;
            }

            if (ExpectedTypeFor(parameterName) is string expectedType &&
                !string.Equals(parameterType, expectedType, StringComparison.OrdinalIgnoreCase))
            {
                yield return new Diagnostic(
                    _id,
                    DiagnosticSeverity.Warning,
                    "Align the parameter type with the Azure Pipelines field it maps to so templates remain schema-compatible.",
                    document.FilePath,
                    Line: null,
                    Column: null);

                yield break;
            }
        }
    }

    private static bool TryGetParameterDeclaration(string[] lines, int index, out string? parameterName, out string? parameterType)
    {
        parameterName = null;
        parameterType = null;

        string trimmed = lines[index].Trim();
        if (trimmed.Length == 0 || trimmed.StartsWith('#'))
        {
            return false;
        }

        if (!trimmed.StartsWith("- name:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string[] parts = trimmed.Split(':', 3, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return false;
        }

        parameterName = parts[1];
        int typeIndex = trimmed.IndexOf("type:", StringComparison.OrdinalIgnoreCase);
        if (typeIndex >= 0)
        {
            string remainder = trimmed[(typeIndex + 5)..].Trim();
            parameterType = remainder.Split(' ', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)[0];
            return true;
        }

        for (int nextIndex = index + 1; nextIndex < lines.Length && nextIndex < index + 3; nextIndex++)
        {
            string nextLine = lines[nextIndex].Trim();
            if (nextLine.Length == 0 || nextLine.StartsWith('#'))
            {
                continue;
            }

            if (nextLine.StartsWith("- name:", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            int nextTypeIndex = nextLine.IndexOf("type:", StringComparison.OrdinalIgnoreCase);
            if (nextTypeIndex >= 0)
            {
                string remainder = nextLine[(nextTypeIndex + 5)..].Trim();
                parameterType = remainder.Split(' ', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)[0];
                return true;
            }
        }

        return false;
    }

    private static string? ExpectedTypeFor(string parameterName)
    {
        return parameterName.ToUpperInvariant() switch
        {
            "CONDITION" or "CONTINUEONERROR" or "ENABLED" => "boolean",
            _ => null,
        };
    }
}
