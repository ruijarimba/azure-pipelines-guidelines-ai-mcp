namespace AzurePipelines.Guidelines.Core;

/// <summary>
/// Resolves local analyser capability metadata for implemented guideline rules.
/// </summary>
public interface IGuidelineAutomationMetadataProvider
{
    /// <summary>
    /// Gets the local automation metadata for the specified guideline.
    /// </summary>
    /// <param name="guidelineId">The guideline identifier.</param>
    /// <returns>The metadata, or <see langword="null"/> when no local metadata exists.</returns>
    public GuidelineAutomationMetadata? GetAutomationMetadata(GuidelineId guidelineId);
}
