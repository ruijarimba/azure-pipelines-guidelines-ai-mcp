using System.Runtime.CompilerServices;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules.Variables;

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

        if (IsVariablesTemplate(document))
        {
            yield break;
        }

        foreach (VariableNode variable in GetBroadScopeVariables(document))
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return new Diagnostic(
                _id,
                DiagnosticSeverity.Error,
                $"Variable '{variable.Name ?? "(unnamed)"}' is declared at pipeline or stage scope. " +
                "Declare variables at job scope or in a variables template.",
                document.FilePath,
                Line: null,
                Column: null);
        }
    }

    private static IEnumerable<VariableNode> GetBroadScopeVariables(PipelineDocument document) =>
        document.Variables.Concat(document.Stages.SelectMany(stage => stage.Variables))
            .Where(variable => variable.Name is not null || variable.Value is not null || variable.Group is not null);

    private static bool IsVariablesTemplate(PipelineDocument document) =>
        document.Variables.Count > 0 &&
        document.Stages.Count == 0 &&
        document.Jobs.Count == 0 &&
        document.Steps.Count == 0 &&
        Path.GetFileNameWithoutExtension(document.FilePath)
            .Contains("variable", StringComparison.OrdinalIgnoreCase);
}
