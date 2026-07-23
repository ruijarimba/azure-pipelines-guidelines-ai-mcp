using AzurePipelines.Guidelines.Mcp;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzurePipelines.Guidelines.Mcp.Host;

/// <summary>
/// Starts the MCP server over HTTP for SSE-compatible clients.
/// </summary>
internal static class SseMcpHost
{
    private static readonly Action<ILogger, string, string, string, Exception?> _logServerListening =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(1, "SseServerListening"),
            "MCP SSE server is listening. Endpoint: {Endpoint}; Ports: {Ports}; URLs: {Urls}");

    /// <summary>
    /// Builds and runs the HTTP MCP host until shutdown is requested.
    /// </summary>
    /// <param name="args">Command-line arguments forwarded to the web host.</param>
    /// <param name="cancellationToken">Token used to stop host startup or shutdown.</param>
    internal static async Task RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        WebApplicationOptions options = new()
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory,
        };
        WebApplicationBuilder builder = WebApplication.CreateBuilder(options);

        string? configuredUrl = builder.Configuration[WebHostDefaults.ServerUrlsKey];
        if (string.IsNullOrWhiteSpace(configuredUrl))
        {
            throw new InvalidOperationException($"The '{WebHostDefaults.ServerUrlsKey}' configuration value is required for SSE mode.");
        }

        builder.WebHost.UseUrls(configuredUrl);
        ConfigureLogging(builder.Logging);

        builder.Services.AddGuidelinesMcp(
            analysisDefaults: McpAnalysisDefaults.FromConfiguration(args))
            .WithHttpTransport();

        WebApplication app = builder.Build();
        app.MapMcp("/mcp");

        await app.StartAsync(cancellationToken).ConfigureAwait(false);

        ILogger<Program> logger = app.Services.GetRequiredService<ILogger<Program>>();
        LogServerListening(logger, "/mcp", app.Urls);

        await app.WaitForShutdownAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Configures console logging so diagnostics are written to standard error.</summary>
    /// <param name="logging">The web host logging builder.</param>
    private static void ConfigureLogging(ILoggingBuilder logging)
    {
        logging.ClearProviders();
        logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
    }

    /// <summary>Logs the endpoint, ports, and URLs bound by the HTTP host.</summary>
    /// <param name="logger">The logger receiving the operational message.</param>
    /// <param name="endpointPath">The MCP endpoint path.</param>
    /// <param name="urls">The URLs reported by the running application.</param>
    private static void LogServerListening(ILogger logger, string endpointPath, ICollection<string> urls)
    {
        if (!logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string boundUrls = string.Join(", ", urls);
        string ports = string.Join(", ", urls.Select(GetPort));
        _logServerListening(logger, endpointPath, ports, boundUrls, null);
    }

    /// <summary>Extracts a port number from a bound URL.</summary>
    /// <param name="url">The URL to inspect.</param>
    /// <returns>The port number, or <c>unknown</c> for an invalid URL.</returns>
    private static string GetPort(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out Uri? address)
            ? address.Port.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : "unknown";
    }
}
