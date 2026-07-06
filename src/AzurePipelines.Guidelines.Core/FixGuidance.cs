namespace AzurePipelines.Guidelines.Core;

/// <summary>
/// Actionable guidance on how to fix a violation of a <see cref="GuidelineDefinition"/>.
/// </summary>
/// <param name="Summary">A concise description of the corrective action.</param>
/// <param name="Before">
/// Optional example showing a non-compliant snippet. May be <see langword="null"/>.
/// </param>
/// <param name="After">
/// Optional example showing a compliant snippet. May be <see langword="null"/>.
/// </param>
public sealed record FixGuidance(string Summary, string? Before, string? After);
