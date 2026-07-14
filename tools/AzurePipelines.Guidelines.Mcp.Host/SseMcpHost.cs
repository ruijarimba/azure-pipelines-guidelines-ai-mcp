using AzurePipelines.Guidelines.Mcp;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzurePipelines.Guidelines.Mcp.Host;

internal static class SseMcpHost
{
    private static readonly Action<ILogger, string, string, string, Exception?> _logServerListening =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(1, "SseServerListening"),
            "MCP SSE server is listening. Endpoint: {Endpoint}; Ports: {Ports}; URLs: {Urls}");

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

        builder.Services.AddGuidelinesMcp().WithHttpTransport();

        WebApplication app = builder.Build();
        app.MapMcp("/mcp");

        await app.StartAsync(cancellationToken).ConfigureAwait(false);

        ILogger<Program> logger = app.Services.GetRequiredService<ILogger<Program>>();
        LogServerListening(logger, "/mcp", app.Urls);

        await app.WaitForShutdownAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void ConfigureLogging(ILoggingBuilder logging)
    {
        logging.ClearProviders();
        logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
    }

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

    private static string GetPort(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out Uri? address)
            ? address.Port.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : "unknown";
    }
}
