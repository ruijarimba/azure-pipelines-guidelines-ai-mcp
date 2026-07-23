using AzurePipelines.Guidelines.Mcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzurePipelines.Guidelines.Mcp.Host;

/// <summary>
/// Starts the MCP server over standard input and standard output.
/// </summary>
internal static class StdioMcpHost
{
    private static readonly Action<ILogger, Exception?> _logServerRunning =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(2, "StdioServerRunning"),
            "MCP stdio server is running. Protocol traffic uses stdout; logs are written to stderr.");

    /// <summary>Builds and runs the stdio MCP host until shutdown is requested.</summary>
    /// <param name="args">Command-line arguments forwarded to the generic host.</param>
    /// <param name="cancellationToken">Token used to stop the host.</param>
    internal static async Task RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        HostApplicationBuilder builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);
        ConfigureLogging(builder.Logging);

        builder.Services.AddGuidelinesMcp(
            analysisDefaults: McpAnalysisDefaults.FromConfiguration(args))
            .WithStdioServerTransport();

        IHost host = builder.Build();

        ILogger<Program> logger = host.Services.GetRequiredService<ILogger<Program>>();
        _logServerRunning(logger, null);

        await host.RunAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Configures console logging without contaminating the MCP stdout stream.</summary>
    /// <param name="logging">The generic host logging builder.</param>
    private static void ConfigureLogging(ILoggingBuilder logging)
    {
        logging.ClearProviders();
        logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
    }
}
