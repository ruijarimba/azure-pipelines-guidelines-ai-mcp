namespace AzurePipelines.Guidelines.Core;

/// <summary>
/// Describes the local analyzer capability for one guideline.
/// </summary>
/// <param name="Status">The reliability of evaluation from YAML.</param>
/// <param name="Reason">Why the guideline has this local automation status.</param>
public sealed record GuidelineAutomationMetadata(
    GuidelineAutomationStatus Status,
    string Reason);
