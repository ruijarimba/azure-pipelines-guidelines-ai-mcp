using System.Runtime.CompilerServices;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules;

/// <summary>
/// ADOG-VARIABLES-001 (consider): Detects variables that have a literal value but are
/// not declared as <c>readonly</c>. When a value should not change after initialization,
/// marking it readonly makes the intent explicit and reduces accidental mutation.
/// </summary>
[RuleMetadata("ADOG-VARIABLES-001", "https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/variables/consider-read-only-variables.md")]
internal sealed class ReadonlyVariableRule : IGuidelineRule
{
    private static readonly GuidelineId _id = new("ADOG-VARIABLES-001");

    /// <inheritdoc/>
    public GuidelineId GuidelineId => _id;

    /// <inheritdoc/>
    public async IAsyncEnumerable<Diagnostic> EvaluateAsync(
        PipelineDocument document,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        foreach (VariableNode variable in document.Variables)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (variable.Group is not null || variable.IsReadOnly || variable.Value is null)
            {
                continue;
            }

            yield return new Diagnostic(
                _id,
                DiagnosticSeverity.Info,
                $"Variable '{variable.Name ?? "(unnamed)"}' is mutable. " +
                "Mark it as readonly when the value should not change after initialization.",
                document.FilePath,
                Line: null,
                Column: null);
        }
    }
}
