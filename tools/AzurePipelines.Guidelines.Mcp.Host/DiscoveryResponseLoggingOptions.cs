namespace AzurePipelines.Guidelines.Mcp.Host;

/// <summary>
/// Controls opt-in logging of safe, static MCP discovery responses.
/// </summary>
/// <remarks>
/// This diagnostic mode intentionally excludes request payloads, errors, and dynamic results.
/// It is disabled unless the <c>MCP_LOG_RESPONSES</c> environment variable is explicitly set to
/// <c>true</c>.
/// </remarks>
internal sealed class DiscoveryResponseLoggingOptions
{
    internal const string EnvironmentVariableName = "MCP_LOG_RESPONSES";

    /// <summary>
    /// Gets a value indicating whether discovery response logging is enabled.
    /// </summary>
    public bool Enabled
    {
        get;
        init;
    }

    /// <summary>
    /// Reads the diagnostic setting from the process environment.
    /// </summary>
    internal static DiscoveryResponseLoggingOptions FromEnvironment()
    {
        return FromValue(Environment.GetEnvironmentVariable(EnvironmentVariableName));
    }

    internal static DiscoveryResponseLoggingOptions FromValue(string? value)
    {
        return new DiscoveryResponseLoggingOptions
        {
            Enabled = bool.TryParse(value, out bool enabled) && enabled,
        };
    }
}
