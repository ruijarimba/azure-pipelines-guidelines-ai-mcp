using System.CommandLine;

// Minimal CLI entry point.
// TODO: Add commands (analyze, rules list, rules show) once Analysis is implemented.

RootCommand rootCommand = new("Azure Pipelines Guidelines static analyzer");

// Placeholder command until implementation
Command analyzeCommand = new("analyze", "Analyze an Azure Pipelines YAML file")
{
    new Argument<string>("path", "Path to the YAML file to analyze")
};

analyzeCommand.SetHandler((string path) =>
{
    Console.WriteLine($"[Placeholder] Would analyze: {path}");
}, analyzeCommand.Arguments.OfType<Argument<string>>().First());

rootCommand.AddCommand(analyzeCommand);

return await rootCommand.InvokeAsync(args);
