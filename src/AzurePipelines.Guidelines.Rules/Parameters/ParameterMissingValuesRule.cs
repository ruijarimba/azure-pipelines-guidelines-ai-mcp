using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules.Parameters;

/// <summary>
/// ADOG-PARAMETERS-002 (do): Detects parameters of type <c>string</c> that have no
/// <c>values:</c> list. When the parameter has a finite set of valid inputs, declaring
/// an explicit <c>values:</c> list makes the constraint visible and enables
/// Azure Pipelines to validate it at queue time.
/// </summary>
[RuleMetadata("ADOG-PARAMETERS-002", "https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/parameters/do-restrict-values.md")]
internal sealed class ParameterMissingValuesRule : IGuidelineRule
{
    private static readonly GuidelineId _id = new("ADOG-PARAMETERS-002");

    // Types whose allowed values can be enumerated meaningfully.
    private static readonly FrozenSet<string> _enumerableTypes =
        new[] { "string" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public GuidelineId GuidelineId => _id;

    /// <inheritdoc/>
    public async IAsyncEnumerable<Diagnostic> EvaluateAsync(
        PipelineDocument document,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        foreach (ParameterNode param in document.Parameters)
        {
            cancellationToken.ThrowIfCancellationRequested();

            bool isEnumerableType = param.Type is null ||
                _enumerableTypes.Contains(param.Type);

            if (isEnumerableType && param.Values.Count == 0)
            {
                yield return new Diagnostic(
                    _id,
                    DiagnosticSeverity.Error,
                    $"Parameter '{param.Name}' has no 'values:' list. " +
                    "Declare an explicit list of allowed values to constrain valid inputs.",
                    document.FilePath,
                    Line: null,
                    Column: null);
            }
        }
    }
}
