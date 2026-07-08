using System.Diagnostics;
using System.Globalization;

namespace AzurePipelines.Guidelines.Core;

/// <summary>
/// Represents a single job inside a stage or at the top level of a pipeline.
/// </summary>
/// <param name="Name">The job identifier.</param>
/// <param name="DisplayName">The optional display name.</param>
/// <param name="TimeoutInMinutes">
/// The job-level timeout, or <see langword="null"/> when not set.
/// </param>
/// <param name="Steps">All steps defined directly on this job.</param>
/// <param name="Variables">Variables defined at job scope.</param>
/// <param name="Condition">The optional condition expression string.</param>
/// <param name="Line">The one-based line number in the source YAML, when available.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record JobNode(
    string? Name,
    string? DisplayName,
    int? TimeoutInMinutes,
    IReadOnlyList<StepNode> Steps,
    IReadOnlyList<VariableNode> Variables,
    string? Condition,
    int? Line)
{
    /// <inheritdoc/>
    public override string ToString() =>
        $"Job '{Name ?? "(unnamed)"}' (line {Line?.ToString(CultureInfo.InvariantCulture) ?? "?"}, {Steps.Count} steps)";
}
