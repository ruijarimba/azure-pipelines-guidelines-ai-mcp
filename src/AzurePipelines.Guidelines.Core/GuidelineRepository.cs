using System.Security.Cryptography;
using System.Text;

namespace AzurePipelines.Guidelines.Core;

/// <summary>
/// An in-memory implementation of <see cref="IGuidelineRepository"/> backed by a fixed
/// list of <see cref="GuidelineDefinition"/> records supplied at construction time.
/// </summary>
public sealed class GuidelineRepository : IGuidelineRepository
{
    private readonly IReadOnlyList<GuidelineDefinition> _guidelines;
    private readonly string _contentVersion;

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
        _contentVersion = ComputeContentVersion(guidelines);
    }

    /// <inheritdoc/>
    public string ContentVersion => _contentVersion;

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

    private static string ComputeContentVersion(IReadOnlyList<GuidelineDefinition> guidelines)
    {
        StringBuilder builder = new();
        for (int index = 0; index < guidelines.Count; index++)
        {
            GuidelineDefinition guideline = guidelines[index];
            builder.Append(guideline.Id.Value);
            builder.Append('|');
            builder.Append(guideline.Category.ToString().ToUpperInvariant());
            builder.Append('|');
            builder.Append(guideline.Severity.ToString().ToUpperInvariant());
            builder.Append('|');
            builder.Append(guideline.Title);
            builder.Append('|');
            builder.Append(guideline.Description);
            builder.Append('\n');
        }

        byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        Span<char> hashChars = stackalloc char[hashBytes.Length * 2];
        for (int i = 0; i < hashBytes.Length; i++)
        {
            hashChars[i * 2] = GetHexDigit(hashBytes[i] >> 4);
            hashChars[(i * 2) + 1] = GetHexDigit(hashBytes[i] & 0x0F);
        }

        return new string(hashChars[..(hashBytes.Length * 2)]);
    }

    private static char GetHexDigit(int value) =>
        value < 10 ? (char)('0' + value) : (char)('a' + (value - 10));
}
