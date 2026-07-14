using Microsoft.Extensions.Logging;

namespace AzurePipelines.Guidelines.Mcp;

/// <summary>
/// High-performance log messages used while loading the guideline manifest.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
internal static partial class LoaderLog
{
    /// <summary>Logs the number of definitions loaded from the manifest.</summary>
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Loaded {Count} guideline definitions from manifest.")]
    internal static partial void GuidelinesLoaded(ILogger logger, int count);

    /// <summary>Logs a manifest-loading failure and the resulting empty repository.</summary>
    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Error,
        Message = "Failed to load guideline manifest. The repository will be empty.")]
    internal static partial void LoadFailed(ILogger logger, Exception exception);
}
