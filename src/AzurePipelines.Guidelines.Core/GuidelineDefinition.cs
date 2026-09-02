namespace AzurePipelines.Guidelines.Core;

/// <summary>
/// The complete, immutable definition of a single Azure Pipelines guideline as loaded
/// from the manifest provided by the companion guidelines repository.
/// </summary>
/// <param name="Id">The stable unique identifier, e.g. <c>ADOG-STEPS-001</c>.</param>
/// <param name="Category">The category this guideline belongs to.</param>
/// <param name="Severity">The recommended-practice strength of this guideline.</param>
/// <param name="Title">A short, human-readable title.</param>
/// <param name="Description">A detailed description of the guideline.</param>
/// <param name="Rationale">
/// The reasoning behind the guideline. May be <see langword="null"/> when absent in the manifest.
/// </param>
/// <param name="Tags">Optional classification tags.</param>
/// <param name="DetectionHints">
/// Machine-readable hints that describe how to detect potential violations.
/// </param>
/// <param name="Fix">Optional fix guidance. May be <see langword="null"/>.</param>
/// <param name="References">External links that provide further reading.</param>
public sealed record GuidelineDefinition(
    GuidelineId Id,
    GuidelineCategory Category,
    GuidelineSeverity Severity,
    string Title,
    string Description,
    string? Rationale,
    IReadOnlyList<string> Tags,
    IReadOnlyList<DetectionHint> DetectionHints,
    FixGuidance? Fix,
    IReadOnlyList<string> References);
