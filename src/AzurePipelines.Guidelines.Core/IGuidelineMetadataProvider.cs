namespace AzurePipelines.Guidelines.Core;

/// <summary>
/// Resolves the canonical documentation reference for an implemented guideline rule.
/// </summary>
public interface IGuidelineMetadataProvider
{
    /// <summary>
    /// Gets the canonical documentation URL for the specified guideline.
    /// </summary>
    /// <param name="guidelineId">The guideline identifier.</param>
    /// <returns>The canonical URL, or <see langword="null"/> when no metadata exists.</returns>
    public string? GetCanonicalReference(GuidelineId guidelineId);
}
