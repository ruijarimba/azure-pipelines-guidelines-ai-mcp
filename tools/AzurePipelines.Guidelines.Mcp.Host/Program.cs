using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Minimal MCP server host entry point.
// Wires up DI and starts listening for AI assistant requests.

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// TODO: Register the MCP server and analysis services once implemented:
// builder.Services.AddGuidelinesAnalysis();
// builder.Services.AddGuidelinesMcp();

IHost host = builder.Build();

await host.RunAsync();
