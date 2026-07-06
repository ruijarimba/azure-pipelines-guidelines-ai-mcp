using AzurePipelines.Guidelines.Mcp;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// MCP servers communicate over stdio: stdout is the protocol channel.
// Redirect all logging to stderr so it does not interfere with MCP messages.
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services.AddGuidelinesMcp();

IHost host = builder.Build();

await host.RunAsync();
