using System.Collections.Frozen;
using System.Reflection;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules;

/// <summary>
/// Resolves canonical guideline URLs from rule metadata declared in the Rules assembly.
/// </summary>
internal sealed class RuleMetadataProvider : IGuidelineMetadataProvider
{
    private readonly FrozenDictionary<string, string> _references = LoadReferences();

    /// <summary>
    /// Gets the canonical documentation URL for a guideline identifier.
    /// </summary>
    /// <param name="guidelineId">The guideline identifier to resolve.</param>
    /// <returns>The metadata URL, or <see langword="null"/> when no rule declares the identifier.</returns>
    public string? GetCanonicalReference(GuidelineId guidelineId) =>
        _references.GetValueOrDefault(guidelineId.Value);

    /// <summary>
    /// Builds an immutable lookup from rule identifiers to their declared documentation URLs.
    /// </summary>
    /// <returns>The canonical rule reference lookup.</returns>
    private static FrozenDictionary<string, string> LoadReferences()
    {
        Dictionary<string, string> references = new(StringComparer.Ordinal);

        foreach (Type type in typeof(RuleMetadataProvider).Assembly.GetTypes())
        {
            RuleMetadataAttribute? metadata = type.GetCustomAttribute<RuleMetadataAttribute>();
            if (metadata is not null)
            {
                references[metadata.RuleId] = metadata.GuidelineUrl;
            }
        }

        return references.ToFrozenDictionary(StringComparer.Ordinal);
    }
}
