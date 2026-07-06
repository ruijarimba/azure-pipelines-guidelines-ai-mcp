namespace AzurePipelines.Guidelines.Core;

/// <summary>
/// Represents a single parameter definition in an Azure Pipelines YAML document.
/// </summary>
/// <param name="Name">The parameter name.</param>
/// <param name="Type">The declared type (e.g. <c>string</c>, <c>boolean</c>, <c>object</c>).</param>
/// <param name="Default">The default value as a raw string, or <see langword="null"/> if not set.</param>
/// <param name="Values">
/// Allowed values when the parameter acts as an enum, or an empty list when unrestricted.
/// </param>
public sealed record ParameterNode(
    string Name,
    string? Type,
    string? Default,
    IReadOnlyList<string> Values);
