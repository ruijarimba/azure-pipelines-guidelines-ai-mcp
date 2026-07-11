using System.Runtime.CompilerServices;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules;

/// <summary>
/// ADOG-VARIABLES-005 (do): Detects variables that are declared at a broader scope than
/// necessary. Restricting variables to the narrowest applicable scope reduces accidental
/// coupling and makes pipelines easier to reason about.
/// </summary>
[RuleMetadata("ADOG-VARIABLES-005", "https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/variables/do-variable-scope.md")]
internal sealed class VariableScopeRule : IGuidelineRule
{
    private static readonly GuidelineId _id = new("ADOG-VARIABLES-005");

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

            if (variable.Value is not null && variable.Group is null)
            {
                yield return new Diagnostic(
                    _id,
                    DiagnosticSeverity.Error,
                    $"Variable '{variable.Name ?? "(unnamed)"}' is declared at pipeline scope. " +
                    "Restrict it to the narrowest scope that still satisfies your pipeline needs.",
                    document.FilePath,
                    Line: null,
                    Column: null);
            }
        }
    }
}
