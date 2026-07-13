using AzurePipelines.Guidelines.Mcp;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Default transport is stdio. SSE is available for debugging so the server can stay
// running inside Visual Studio while VS Code (or another MCP client) connects over HTTP.
string transport = GetTransport(args);

if (string.Equals(transport, "sse", StringComparison.OrdinalIgnoreCase))
{
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
    ConfigureLogging(builder.Logging);

    builder.Services.AddGuidelinesMcp().WithHttpTransport();

    WebApplication app = builder.Build();
    app.MapMcp("/mcp");

    await app.RunAsync().ConfigureAwait(false);
}
else
{
    // MCP servers communicate over stdio: stdout is the protocol channel.
    // Redirect all logging to stderr so it does not interfere with MCP messages.
    HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
    ConfigureLogging(builder.Logging);

    builder.Services.AddGuidelinesMcp().WithStdioServerTransport();

    IHost host = builder.Build();

    await host.RunAsync().ConfigureAwait(false);
}

static void ConfigureLogging(ILoggingBuilder logging)
{
    logging.ClearProviders();
    logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
}

static string GetTransport(string[] args)
{
    for (int index = 0; index < args.Length; index++)
    {
        string argument = args[index];
        if (!string.Equals(argument, "--transport", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        if (index + 1 < args.Length)
        {
            return args[index + 1];
        }

        break;
    }

    string? environmentTransport = Environment.GetEnvironmentVariable("MCP_TRANSPORT");
    return string.IsNullOrWhiteSpace(environmentTransport) ? "stdio" : environmentTransport;
}
