using System.Diagnostics;

namespace AzurePipelines.Guidelines.Core;

/// <summary>
/// An in-memory representation of an Azure Pipelines YAML document, ready for analysis.
/// </summary>
/// <param name="FilePath">
/// The file-system path (or virtual path) of the pipeline file, e.g.
/// <c>azure-pipelines.yml</c>. Used in diagnostic messages.
/// </param>
/// <param name="RawContent">The original YAML text that was read from the file.</param>
/// <param name="Parameters">Top-level parameter definitions, in declaration order.</param>
/// <param name="Variables">Top-level variable definitions, in declaration order.</param>
/// <param name="Stages">
/// Stages declared at the top level. Empty when the pipeline uses the
/// <c>jobs:</c> or <c>steps:</c> shorthand rather than explicit stages.
/// </param>
/// <param name="Jobs">
/// Jobs declared at the top level (stage-less pipeline). Empty when the
/// pipeline uses explicit stages or the <c>steps:</c> shorthand.
/// </param>
/// <param name="Steps">
/// Steps declared at the top level (steps-only template). Empty when the
/// pipeline uses jobs or stages.
/// </param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record PipelineDocument(
    string FilePath,
    [property: DebuggerBrowsable(DebuggerBrowsableState.Never)]
    string RawContent,
    IReadOnlyList<ParameterNode> Parameters,
    IReadOnlyList<VariableNode> Variables,
    IReadOnlyList<StageNode> Stages,
    IReadOnlyList<JobNode> Jobs,
    IReadOnlyList<StepNode> Steps)
{
    /// <summary>
    /// Returns all jobs across all stages plus any top-level jobs.
    /// </summary>
    public IEnumerable<JobNode> AllJobs =>
        Stages.SelectMany(s => s.Jobs).Concat(Jobs);

    /// <summary>
    /// Returns all steps across all jobs and stages, plus any top-level steps.
    /// </summary>
    public IEnumerable<StepNode> AllSteps =>
        AllJobs.SelectMany(j => j.Steps).Concat(Steps);

    /// <inheritdoc/>
    public override string ToString() =>
        $"{FilePath} ({Stages.Count} stages, {Jobs.Count} top-level jobs, {Steps.Count} top-level steps)";
}
