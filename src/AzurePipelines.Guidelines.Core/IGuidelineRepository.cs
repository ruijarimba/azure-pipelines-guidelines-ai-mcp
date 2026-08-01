namespace AzurePipelines.Guidelines.Core;

/// <summary>
/// Provides access to the loaded set of Azure Pipelines guideline definitions.
/// </summary>
public interface IGuidelineRepository
{
    /// <summary>
    /// Returns the stable fingerprint for the currently loaded guideline content.
    /// </summary>
    public string ContentVersion { get; }

    /// <summary>
    /// Returns all loaded guideline definitions.
    /// </summary>
    public IReadOnlyList<GuidelineDefinition> GetAll();

    /// <summary>
    /// Returns the guideline definition with the given <paramref name="id"/>,
    /// or <see langword="null"/> if not found.
    /// </summary>
    /// <param name="id">The stable guideline identifier to look up.</param>
    public GuidelineDefinition? FindById(GuidelineId id);

    /// <summary>
    /// Returns all guideline definitions that belong to the given <paramref name="category"/>.
    /// </summary>
    /// <param name="category">The category to filter by.</param>
    public IReadOnlyList<GuidelineDefinition> GetByCategory(GuidelineCategory category);
}
