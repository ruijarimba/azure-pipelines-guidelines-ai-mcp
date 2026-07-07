using System.CommandLine;
using AzurePipelines.Guidelines.Analysis;
using AzurePipelines.Guidelines.Cli;
using AzurePipelines.Guidelines.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Load the guideline catalogue once at startup (before building the DI container
// so the repository can be shared by both the analyser and the rules commands).
IGuidelineRepository repository = await LoadGuidelinesAsync();

// Wire services without a full IHost — keeps startup fast for a CLI tool.
ServiceCollection services = new();
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
services.AddSingleton(repository);
services.AddGuidelinesAnalysis();

await using ServiceProvider sp = services.BuildServiceProvider();

IPipelineParser parser = sp.GetRequiredService<IPipelineParser>();
IPipelineAnalyser analyser = sp.GetRequiredService<IPipelineAnalyser>();
PipelinePathResolver pathResolver = sp.GetRequiredService<PipelinePathResolver>();

RootCommand rootCommand = new("Azure Pipelines Guidelines static analyser (adog)");
rootCommand.AddCommand(AnalyzeCommand.Create(parser, analyser, pathResolver));
rootCommand.AddCommand(RulesCommand.Create(repository));

return await rootCommand.InvokeAsync(args);

static async Task<IGuidelineRepository> LoadGuidelinesAsync()
{
    using HttpClient httpClient = new();
    httpClient.DefaultRequestHeaders.Add("User-Agent", "adog/1.0");
    HttpGuidelineLoader loader = new(httpClient);

    try
    {
        IReadOnlyList<GuidelineDefinition> guidelines = await loader.LoadAsync().ConfigureAwait(false);
        return new GuidelineRepository(guidelines);
    }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
    {
        await Console.Error.WriteLineAsync(
            $"warning: Failed to load guideline catalogue: {ex.Message} " +
            "— 'rules' commands will return no results.")
            .ConfigureAwait(false);
        return new GuidelineRepository([]);
    }
}

