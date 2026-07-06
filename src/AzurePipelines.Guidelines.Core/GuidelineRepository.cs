namespace AzurePipelines.Guidelines.Core;

/// <summary>
/// An in-memory implementation of <see cref="IGuidelineRepository"/> backed by a fixed
/// list of <see cref="GuidelineDefinition"/> records supplied at construction time.
/// </summary>
public sealed class GuidelineRepository : IGuidelineRepository
{
    private readonly IReadOnlyList<GuidelineDefinition> _guidelines;

    /// <summary>
    /// Initialises a new <see cref="GuidelineRepository"/> with the given guidelines.
    /// </summary>
    /// <param name="guidelines">
    /// The guideline definitions to store. Must not be <see langword="null"/>.
    /// Duplicate IDs are permitted by this class; callers are responsible for
    /// ensuring uniqueness if required.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="guidelines"/> is <see langword="null"/>.
    /// </exception>
    public GuidelineRepository(IReadOnlyList<GuidelineDefinition> guidelines)
    {
        ArgumentNullException.ThrowIfNull(guidelines);
        _guidelines = guidelines;
    }

    /// <inheritdoc/>
    public IReadOnlyList<GuidelineDefinition> GetAll() => _guidelines;

    /// <inheritdoc/>
    public GuidelineDefinition? FindById(GuidelineId id)
    {
        ArgumentNullException.ThrowIfNull(id);

        foreach (GuidelineDefinition guideline in _guidelines)
        {
            if (guideline.Id.Equals(id))
            {
                return guideline;
            }
        }

        return null;
    }

    /// <inheritdoc/>
    public IReadOnlyList<GuidelineDefinition> GetByCategory(GuidelineCategory category)
    {
        List<GuidelineDefinition> results = [];

        foreach (GuidelineDefinition guideline in _guidelines)
        {
            if (guideline.Category == category)
            {
                results.Add(guideline);
            }
        }

        return results;
    }
}
