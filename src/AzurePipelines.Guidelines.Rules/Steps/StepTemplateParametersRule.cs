using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules.Steps;

/// <summary>
/// ADOG-STEPS-007 (consider): Detects reusable step templates that rely on control settings
/// such as pool, condition, or dependsOn without exposing them as parameters.
/// </summary>
[RuleMetadata("ADOG-STEPS-007", "https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/do-extensible-steps.md")]
internal sealed class StepTemplateParametersRule : IGuidelineRule
{
    private static readonly GuidelineId _id = new("ADOG-STEPS-007");
    private static readonly string[] _controlParameters =
    [
        "condition",
        "continueOnError",
        "enabled",
        "retryCountOnTaskFailure",
        "timeoutInMinutes"
    ];

    /// <inheritdoc/>
    public GuidelineId GuidelineId => _id;

    /// <inheritdoc/>
    public async IAsyncEnumerable<Diagnostic> EvaluateAsync(
        PipelineDocument document,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        string[] lines = document.RawContent.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        List<string> missingParameters = FindMissingParameters(lines);

        if (missingParameters.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return new Diagnostic(
                _id,
                DiagnosticSeverity.Info,
                $"Expose step-template control settings as parameters: {string.Join(", ", missingParameters)}.",
                document.FilePath,
                Line: null,
                Column: null);
        }
    }

    private static List<string> FindMissingParameters(IReadOnlyList<string> lines)
    {
        HashSet<string> usedControls = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> declaredParameters = new(StringComparer.OrdinalIgnoreCase);
        bool inParameters = false;

        foreach (string line in lines)
        {
            string trimmed = line.Trim();
            if (trimmed.Equals("parameters:", StringComparison.OrdinalIgnoreCase))
            {
                inParameters = true;
                continue;
            }

            if (inParameters && line.Length - line.TrimStart().Length == 0)
            {
                inParameters = false;
            }

            Match setting = Regex.Match(trimmed, "^(?<name>condition|continueOnError|enabled|retryCountOnTaskFailure|timeoutInMinutes):", RegexOptions.IgnoreCase);
            if (!setting.Success)
            {
                continue;
            }

            string name = setting.Groups["name"].Value;
            if (inParameters)
            {
                declaredParameters.Add(name);
            }
            else
            {
                usedControls.Add(name);
            }
        }

        return _controlParameters
            .Where(usedControls.Contains)
            .Where(name => !declaredParameters.Contains(name))
            .ToList();
    }
}
