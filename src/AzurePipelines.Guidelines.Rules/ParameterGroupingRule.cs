using System.Runtime.CompilerServices;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules;

/// <summary>
/// ADOG-PARAMETERS-001 (consider): Detects parameters that are declared in a way that
/// mixes unrelated concerns (for example a username/password pair) without grouping them
/// into a structured object or grouped parameter block.
/// </summary>
[RuleMetadata("ADOG-PARAMETERS-001", "https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/parameters/consider-grouping.md")]
internal sealed class ParameterGroupingRule : IGuidelineRule
{
    private static readonly GuidelineId _id = new("ADOG-PARAMETERS-001");

    /// <inheritdoc/>
    public GuidelineId GuidelineId => _id;

    /// <inheritdoc/>
    public async IAsyncEnumerable<Diagnostic> EvaluateAsync(
        PipelineDocument document,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        foreach (ParameterNode parameter in document.Parameters)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (parameter.Name.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                parameter.Name.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                parameter.Name.Contains("secret", StringComparison.OrdinalIgnoreCase))
            {
                string parameterName = RuleHelpers.SanitizeForDiagnostic(parameter.Name);

                yield return new Diagnostic(
                    _id,
                    DiagnosticSeverity.Info,
                    $"Parameter '{parameterName}' looks like a sensitive value. " +
                    "Consider grouping related parameters together so the interface is easier to understand.",
                    document.FilePath,
                    Line: null,
                    Column: null);
            }
        }
    }
}
