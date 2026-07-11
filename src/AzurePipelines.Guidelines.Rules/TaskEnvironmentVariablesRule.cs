using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules;

/// <summary>
/// ADOG-STEPS-002 (consider): Detects task or script steps that reference variables but do
/// not declare environment variables at the step level. Declaring env values at the step
/// level makes the dependency explicit and easier to tune per task.
/// </summary>
[RuleMetadata("ADOG-STEPS-002", "https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/consider-environment-variables.md")]
internal sealed partial class TaskEnvironmentVariablesRule : IGuidelineRule
{
    private static readonly GuidelineId _id = new("ADOG-STEPS-002");

    /// <inheritdoc/>
    public GuidelineId GuidelineId => _id;

    /// <inheritdoc/>
    public async IAsyncEnumerable<Diagnostic> EvaluateAsync(
        PipelineDocument document,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        string[] lines = document.RawContent
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n');

        for (int index = 0; index < lines.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string line = lines[index];
            if (!LooksLikeStepStart(line))
            {
                continue;
            }

            int stepIndentation = line.Length - line.TrimStart().Length;
            int blockEnd = FindBlockEnd(lines, index + 1, stepIndentation);

            string block = string.Join("\n", lines[index..blockEnd]);
            if (VariableReferencePattern().IsMatch(block) &&
                !ContainsEnvironmentDeclaration(block))
            {
                yield return new Diagnostic(
                    _id,
                    DiagnosticSeverity.Info,
                    "This step uses variables but does not declare environment values at the step level. Consider adding an env block for clarity.",
                    document.FilePath,
                    index + 1,
                    Column: null);
            }
        }
    }

    private static bool LooksLikeStepStart(string line)
    {
        string trimmed = line.Trim();
        if (trimmed.Length == 0 || trimmed.StartsWith('#'))
        {
            return false;
        }

        return trimmed.StartsWith("- script:", StringComparison.Ordinal)
            || trimmed.StartsWith("- task:", StringComparison.Ordinal)
            || trimmed.StartsWith("- pwsh:", StringComparison.Ordinal)
            || trimmed.StartsWith("- bash:", StringComparison.Ordinal)
            || trimmed.StartsWith("- powershell:", StringComparison.Ordinal)
            || trimmed.StartsWith("script:", StringComparison.Ordinal)
            || trimmed.StartsWith("task:", StringComparison.Ordinal)
            || trimmed.StartsWith("pwsh:", StringComparison.Ordinal)
            || trimmed.StartsWith("bash:", StringComparison.Ordinal)
            || trimmed.StartsWith("powershell:", StringComparison.Ordinal);
    }

    private static bool ContainsEnvironmentDeclaration(string block)
        => block.Contains("env:", StringComparison.OrdinalIgnoreCase)
            || block.Contains("environment:", StringComparison.OrdinalIgnoreCase);

    private static int FindBlockEnd(string[] lines, int startIndex, int indentation)
    {
        for (int index = startIndex; index < lines.Length; index++)
        {
            string line = lines[index];
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
            {
                continue;
            }

            int currentIndentation = line.Length - line.TrimStart().Length;
            if (currentIndentation <= indentation)
            {
                return index;
            }
        }

        return lines.Length;
    }

    [GeneratedRegex(@"\$\([^)]+\)|\$\{[^}]+\}", RegexOptions.CultureInvariant)]
    private static partial Regex VariableReferencePattern();
}
