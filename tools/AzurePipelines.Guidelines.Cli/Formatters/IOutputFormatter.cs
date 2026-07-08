using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Cli.Formatters;

/// <summary>
/// Formats analysis results for output.
/// </summary>
internal interface IOutputFormatter
{
    /// <summary>
    /// Gets the format name (e.g., "console", "json", "junit").
    /// Used for <c>--format</c> option matching (case-insensitive).
    /// </summary>
    public string FormatName { get; }

    /// <summary>
    /// Formats the analysis results from one or more files into the target output format.
    /// </summary>
    /// <param name="results">The analysis results to format. Must not be null or empty.</param>
    /// <param name="useColor">
    /// Whether to include ANSI color codes in the output (ignored by formatters that don't support color).
    /// Typically driven by <c>--no-color</c> flag and TTY detection.
    /// </param>
    /// <returns>The formatted output as a string.</returns>
    public string Format(IReadOnlyList<AnalysisResult> results, bool useColor = true);
}
