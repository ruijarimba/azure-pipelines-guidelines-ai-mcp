namespace AzurePipelines.Guidelines.Core;

/// <summary>
/// Severity level of a diagnostic produced by the analysis engine.
/// Values are ordered from least to most severe so that threshold comparisons
/// use the natural <c>&gt;=</c> operator (e.g., <c>severity &gt;= DiagnosticSeverity.Warning</c>).
/// </summary>
public enum DiagnosticSeverity
{
    /// <summary>
    /// An informational finding. Maps to <see cref="GuidelineSeverity.Consider"/>.
    /// </summary>
    Info,

    /// <summary>
    /// A likely violation that should be reviewed. Maps to <see cref="GuidelineSeverity.Avoid"/>.
    /// </summary>
    Warning,

    /// <summary>
    /// A violation that must be fixed. Maps to <see cref="GuidelineSeverity.Do"/>
    /// and <see cref="GuidelineSeverity.DoNot"/>.
    /// </summary>
    Error,
}
