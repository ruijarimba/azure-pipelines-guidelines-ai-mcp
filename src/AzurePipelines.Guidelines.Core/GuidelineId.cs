using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AzurePipelines.Guidelines.Core;

/// <summary>
/// Stable unique identifier for a guideline in the form <c>ADOG-{CATEGORY}-{NNN}</c>,
/// for example <c>ADOG-STEPS-001</c>. Validates the format at construction time.
/// </summary>
[DebuggerDisplay("{Value,nq}")]
public sealed class GuidelineId : IEquatable<GuidelineId>
{
    private static readonly Regex _validPattern = new(
        @"^ADOG-(GENERAL|JOBS|PARAMETERS|PIPELINES|STAGES|STEPS|VARIABLES)-[0-9]{3}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Gets the raw string value of this identifier.</summary>
    public string Value { get; }

    /// <summary>
    /// Initialises a new <see cref="GuidelineId"/> and validates the format.
    /// </summary>
    /// <param name="value">
    /// The raw identifier string, for example <c>ADOG-STEPS-001</c>.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value"/> is null, empty, whitespace, or does not match
    /// the <c>ADOG-{CATEGORY}-{NNN}</c> pattern.
    /// </exception>
    public GuidelineId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (!_validPattern.IsMatch(value))
        {
            throw new ArgumentException(
                $"'{value}' is not a valid guideline ID. " +
                "Expected format: ADOG-{CATEGORY}-{NNN} where CATEGORY is one of " +
                "GENERAL, JOBS, PARAMETERS, PIPELINES, STAGES, STEPS, or VARIABLES " +
                "and NNN is a zero-padded three-digit number.",
                nameof(value));
        }

        Value = value;
    }

    /// <inheritdoc/>
    public bool Equals(GuidelineId? other) => other is not null && Value == other.Value;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is GuidelineId other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    /// <summary>Returns the raw string value of this identifier.</summary>
    public override string ToString() => Value;
}
