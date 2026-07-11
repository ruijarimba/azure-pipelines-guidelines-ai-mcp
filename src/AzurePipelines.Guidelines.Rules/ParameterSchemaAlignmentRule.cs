using System.Runtime.CompilerServices;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules;

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
        foreach (string line in content.Split('\n'))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!LooksLikeParameterDeclaration(line, out string? parameterName, out string? parameterType))
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

    private static bool LooksLikeParameterDeclaration(string line, out string? parameterName, out string? parameterType)
    {
        parameterName = null;
        parameterType = null;

        string trimmed = line.Trim();
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
        if (typeIndex < 0)
        {
            return false;
        }

        string remainder = trimmed[(typeIndex + 5)..].Trim();
        parameterType = remainder.Split(' ', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)[0];
        return true;
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
