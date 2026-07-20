using System.Runtime.CompilerServices;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules.Steps;

/// <summary>
/// ADOG-STEPS-004 (consider): Detects scripts and tasks that do not appear to log enough
/// diagnostic context to make failures easy to troubleshoot.
/// </summary>
[RuleMetadata("ADOG-STEPS-004", "https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/consider-logging-diagnostics.md")]
internal sealed class DiagnosticLoggingConsiderationRule : IGuidelineRule
{
    private static readonly GuidelineId _id = new("ADOG-STEPS-004");

    /// <inheritdoc/>
    public GuidelineId GuidelineId => _id;

    /// <inheritdoc/>
    public async IAsyncEnumerable<Diagnostic> EvaluateAsync(
        PipelineDocument document,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        string content = document.RawContent.Replace("\r\n", "\n", StringComparison.Ordinal);
        bool hasStepContent = content.Contains("script:", StringComparison.OrdinalIgnoreCase)
            || content.Contains("pwsh:", StringComparison.OrdinalIgnoreCase)
            || content.Contains("bash:", StringComparison.OrdinalIgnoreCase)
            || content.Contains("powershell:", StringComparison.OrdinalIgnoreCase);
        bool hasLogging = content.Contains("echo ", StringComparison.OrdinalIgnoreCase)
            || content.Contains("Write-Host", StringComparison.OrdinalIgnoreCase)
            || content.Contains("Write-Output", StringComparison.OrdinalIgnoreCase)
            || content.Contains("Write-Information", StringComparison.OrdinalIgnoreCase)
            || content.Contains("printf ", StringComparison.OrdinalIgnoreCase)
            || content.Contains("logger", StringComparison.OrdinalIgnoreCase);

        if (hasStepContent && !hasLogging)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return new Diagnostic(
                _id,
                DiagnosticSeverity.Warning,
                "Add clear diagnostic logging to scripts and tasks so failures are easier to troubleshoot.",
                document.FilePath,
                Line: null,
                Column: null);
        }
    }
}
