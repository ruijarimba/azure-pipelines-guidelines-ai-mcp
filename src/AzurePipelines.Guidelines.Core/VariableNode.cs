namespace AzurePipelines.Guidelines.Core;

/// <summary>
/// Represents a single variable definition in an Azure Pipelines YAML document.
/// </summary>
/// <param name="Name">
/// The variable name when declared with <c>name: value</c> syntax,
/// or <see langword="null"/> for group/template references.
/// </param>
/// <param name="Value">
/// The raw string value, or <see langword="null"/> for group/template references.
/// </param>
/// <param name="IsReadOnly">
/// <see langword="true"/> when the variable is declared with <c>readonly: true</c>.
/// </param>
/// <param name="Group">
/// The variable group name when this entry is a <c>group:</c> reference.
/// </param>
public sealed record VariableNode(
    string? Name,
    string? Value,
    bool IsReadOnly,
    string? Group);
