namespace AzurePipelines.Guidelines.Core;

/// <summary>Extension methods for <see cref="GuidelineSeverity"/>.</summary>
public static class GuidelineSeverityExtensions
{
    /// <summary>
    /// Maps a <see cref="GuidelineSeverity"/> to its corresponding <see cref="DiagnosticSeverity"/>
    /// as defined in the severity mapping table in <c>docs/glossary.md</c>.
    /// </summary>
    /// <param name="severity">The guideline severity to map.</param>
    /// <returns>The corresponding <see cref="DiagnosticSeverity"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="severity"/> is not a defined <see cref="GuidelineSeverity"/> value.
    /// </exception>
    public static DiagnosticSeverity ToDiagnosticSeverity(this GuidelineSeverity severity) =>
        severity switch
        {
            GuidelineSeverity.Do => DiagnosticSeverity.Error,
            GuidelineSeverity.DoNot => DiagnosticSeverity.Error,
            GuidelineSeverity.Avoid => DiagnosticSeverity.Warning,
            GuidelineSeverity.Consider => DiagnosticSeverity.Info,
            _ => throw new ArgumentOutOfRangeException(
                nameof(severity), severity, $"Unexpected {nameof(GuidelineSeverity)} value: {severity}."),
        };
}
