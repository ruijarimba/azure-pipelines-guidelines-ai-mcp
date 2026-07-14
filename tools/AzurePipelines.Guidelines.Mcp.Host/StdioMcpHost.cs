using AzurePipelines.Guidelines.Mcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzurePipelines.Guidelines.Mcp.Host;

internal static class StdioMcpHost
{
    private static readonly Action<ILogger, Exception?> _logServerRunning =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(2, "StdioServerRunning"),
            "MCP stdio server is running. Protocol traffic uses stdout; logs are written to stderr.");

    internal static async Task RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        HostApplicationBuilder builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);
        ConfigureLogging(builder.Logging);

        builder.Services.AddGuidelinesMcp().WithStdioServerTransport();

        IHost host = builder.Build();

        ILogger<Program> logger = host.Services.GetRequiredService<ILogger<Program>>();
        _logServerRunning(logger, null);

        await host.RunAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void ConfigureLogging(ILoggingBuilder logging)
    {
        logging.ClearProviders();
        logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
    }
}
