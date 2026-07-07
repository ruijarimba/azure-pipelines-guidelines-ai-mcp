using System.CommandLine;
using AzurePipelines.Guidelines.Analysis;
using AzurePipelines.Guidelines.Cli;
using AzurePipelines.Guidelines.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Wire services without a full IHost — keeps startup fast for a CLI tool.
ServiceCollection services = new();
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
services.AddGuidelinesAnalysis();

await using ServiceProvider sp = services.BuildServiceProvider();

IPipelineParser parser = sp.GetRequiredService<IPipelineParser>();
IPipelineAnalyser analyser = sp.GetRequiredService<IPipelineAnalyser>();

RootCommand rootCommand = new("Azure Pipelines Guidelines static analyser (adog)");
rootCommand.AddCommand(AnalyzeCommand.Create(parser, analyser));

return await rootCommand.InvokeAsync(args);
