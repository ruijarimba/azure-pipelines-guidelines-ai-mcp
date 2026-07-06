namespace AzurePipelines.Guidelines.Core;

/// <summary>
/// Represents a single stage in an Azure Pipelines YAML document.
/// </summary>
/// <param name="Name">The stage identifier.</param>
/// <param name="DisplayName">The optional display name.</param>
/// <param name="Jobs">All jobs defined directly on this stage.</param>
/// <param name="Variables">Variables defined at stage scope.</param>
/// <param name="Condition">The optional condition expression string.</param>
/// <param name="Line">The one-based line number in the source YAML, when available.</param>
public sealed record StageNode(
    string? Name,
    string? DisplayName,
    IReadOnlyList<JobNode> Jobs,
    IReadOnlyList<VariableNode> Variables,
    string? Condition,
    int? Line);
