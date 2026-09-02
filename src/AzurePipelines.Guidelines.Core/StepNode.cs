using System.Diagnostics;
using System.Globalization;

namespace AzurePipelines.Guidelines.Core;

/// <summary>
/// Represents a single step inside a job or steps template.
/// </summary>
/// <param name="Task">
/// The task identifier (e.g. <c>AzureCLI@2</c>) when the step is a <c>task:</c> step,
/// or <see langword="null"/> for script/bash/pwsh/checkout/template steps.
/// </param>
/// <param name="Script">The inline script body for <c>script:</c> steps.</param>
/// <param name="DisplayName">The optional display name of the step.</param>
/// <param name="TimeoutInMinutes">
/// The step-level timeout, or <see langword="null"/> when not set.
/// </param>
/// <param name="IsCheckout">
/// <see langword="true"/> when the step is a <c>checkout:</c> step.
/// </param>
/// <param name="Condition">The optional condition expression string.</param>
/// <param name="Line">The one-based line number in the source YAML, when available.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record StepNode(
    string? Task,
    string? Script,
    string? DisplayName,
    int? TimeoutInMinutes,
    bool IsCheckout,
    string? Condition,
    int? Line)
{
    /// <inheritdoc/>
    public override string ToString() =>
        $"Step {(IsCheckout ? "checkout" : Task ?? "script")} '{DisplayName ?? "(unnamed)"}' (line {Line?.ToString(CultureInfo.InvariantCulture) ?? "?"})";
}
