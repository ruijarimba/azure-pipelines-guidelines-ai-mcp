using AzurePipelines.Guidelines.Mcp;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace AzurePipelines.Guidelines.Mcp.Host;

// Holds transport-specific startup wiring.
// Splitting this from Program.cs keeps the entry point small and makes it clear which
// host type (ASP.NET Core for SSE, generic host for stdio) each transport requires.
internal static class McpHostStartup
{
    // Source-generated log templates avoid runtime string interpolation and remain
    // compatible with high-performance logging analyzers.
    private static readonly Action<ILogger, string, string, Exception?> _logSseServerListening =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(1, "SseServerListening"),
            "MCP HTTP server is listening. Endpoint path: {Endpoint}; URLs: {Urls}. " +
            "Serves Streamable HTTP by default with legacy SSE enabled for backward compatibility.");

    private static readonly Action<ILogger, Exception?> _logStdioServerRunning =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(2, "StdioServerRunning"),
            "MCP stdio server is running. Protocol traffic uses stdout; logs are written to stderr.");

    internal static async Task RunSseAsync(string[] args, CancellationToken cancellationToken = default)
    {
        // ASP.NET Core reads the launch profile's applicationUrl automatically when the profile
        // is selected in Visual Studio or via --launch-profile SSE. When run without that profile
        // the server falls back to the default URL; use --urls or the launch profile to pin 5050.
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        ConfigureLogging(builder.Logging);

        // MCP 2.0 serves the modern Streamable HTTP transport on this endpoint by default.
        // EnableLegacySse keeps the pre-2.0 SSE transport available on the same endpoint so
        // existing SSE-only clients (e.g. older IDE integrations) keep working unchanged.
        // The SDK marks this option obsolete (MCP9004) because legacy SSE has no built-in
        // request backpressure; that risk is acceptable here because this transport is
        // documented as local-debugging-only, running as a trusted, isolated local process
        // (see the "sse" case in Program.cs and the comment on RunSseAsync's caller).
        // Legacy SSE also requires in-memory session state shared between the GET /sse and
        // POST /message requests, so MCP 2.0's new stateless-by-default mode must be disabled.
#pragma warning disable MCP9004 // legacy SSE is intentionally opt-in for trusted local debugging only
        builder.Services.AddGuidelinesMcp()
            .WithHttpTransport(options =>
            {
                options.Stateless = false;
                options.EnableLegacySse = true;
            });
#pragma warning restore MCP9004

        WebApplication app = builder.Build();
        app.MapMcp("/mcp");

        await app.StartAsync(cancellationToken).ConfigureAwait(false);

        ILogger<Program> logger = app.Services.GetRequiredService<ILogger<Program>>();
        LogSseServerListening(logger, "/mcp", app.Urls);

        await app.WaitForShutdownAsync(cancellationToken).ConfigureAwait(false);
    }

    internal static async Task RunStdioAsync(string[] args, CancellationToken cancellationToken = default)
    {
        // In stdio mode stdout is the MCP protocol channel. Any normal log line written there
        // would be interpreted as a malformed MCP message and break the client connection.
        // We therefore force every log level to stderr and leave stdout exclusively for MCP.
        HostApplicationBuilder builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);
        ConfigureLogging(builder.Logging);

        builder.Services.AddGuidelinesMcp().WithStdioServerTransport();

        IHost host = builder.Build();

        ILogger<Program> logger = host.Services.GetRequiredService<ILogger<Program>>();
        _logStdioServerRunning(logger, null);

        await host.RunAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void ConfigureLogging(ILoggingBuilder logging)
    {
        logging.ClearProviders();
        logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
    }

    private static void LogSseServerListening(ILogger logger, string endpointPath, ICollection<string> urls)
    {
        if (!logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        _logSseServerListening(logger, endpointPath, string.Join(", ", urls), null);
    }
}
