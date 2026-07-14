using AzurePipelines.Guidelines.Mcp;
using Microsoft.AspNetCore.Builder;
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
        // ASP.NET Core reads the launch profile's applicationUrl automatically when the profile
        // is selected in Visual Studio or via --launch-profile SSE. When run without that profile
        // the server falls back to the default URL; use --urls or the launch profile to pin 5050.
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
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
