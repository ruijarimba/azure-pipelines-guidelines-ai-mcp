using System.Collections.Frozen;

namespace AzurePipelines.Guidelines.Cli.Formatters;

/// <summary>
/// Factory for resolving <see cref="IOutputFormatter"/> instances by format name.
/// </summary>
internal static class OutputFormatterFactory
{
    // Formatters will be registered here as they are implemented
    private static readonly FrozenDictionary<string, IOutputFormatter> _formatters =
        new Dictionary<string, IOutputFormatter>(StringComparer.OrdinalIgnoreCase)
        {
            ["console"] = new ConsoleOutputFormatter(),
            ["compact"] = new CompactFormatter(),
            ["json"] = new JsonAnalysisFormatter(),
            ["junit"] = new JunitFormatter(),
            ["sarif"] = new SarifFormatter(),
            ["markdown"] = new MarkdownFormatter(),
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets all supported format names (e.g., "console", "json", "junit").
    /// </summary>
    internal static IEnumerable<string> SupportedFormats => _formatters.Keys;

    /// <summary>
    /// Gets the formatter for the specified format name.
    /// </summary>
    /// <param name="format">The format name (case-insensitive).</param>
    /// <returns>The formatter instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the format is not recognized.</exception>
    internal static IOutputFormatter Get(string format)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(format);

        if (!_formatters.TryGetValue(format, out IOutputFormatter? formatter))
        {
            string supportedList = string.Join(", ", _formatters.Keys.Order());
            throw new ArgumentException(
                $"Unknown format '{format}'. Supported formats: {supportedList}",
                nameof(format));
        }

        return formatter;
    }

    /// <summary>
    /// Checks if the specified format is supported.
    /// </summary>
    /// <param name="format">The format name (case-insensitive).</param>
    /// <returns>True if the format is supported; otherwise, false.</returns>
    internal static bool IsSupported(string format)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return false;
        }

        return _formatters.ContainsKey(format);
    }
}
