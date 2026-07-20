namespace AzurePipelines.Guidelines.Core;

/// <summary>
/// Describes a guideline rule that an analysis run did not evaluate.
/// </summary>
/// <param name="Id">The stable guideline identifier.</param>
/// <param name="Status">The rule's local automation status.</param>
/// <param name="Reason">Why the rule was skipped.</param>
public sealed record SkippedGuideline(
    GuidelineId Id,
    GuidelineAutomationStatus Status,
    string Reason);
