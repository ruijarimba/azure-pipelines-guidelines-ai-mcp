namespace AzurePipelines.Guidelines.Core;

/// <summary>
/// Loads guideline definitions from an external data source (e.g. a remote JSON manifest)
/// and makes them available for use.
/// </summary>
public interface IGuidelineLoader
{
    /// <summary>
    /// Asynchronously loads all guideline definitions and returns them.
    /// </summary>
    /// <param name="cancellationToken">Token that can cancel the operation.</param>
    /// <returns>The loaded guidelines, in the order they appear in the source.</returns>
    public Task<IReadOnlyList<GuidelineDefinition>> LoadAsync(CancellationToken cancellationToken = default);
}
