using AzurePipelines.Guidelines.Analysis;
using AzurePipelines.Guidelines.Core;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AzurePipelines.Guidelines.Integration.Tests;

public abstract class PipelineRepositoryIntegrationTestsBase
{
    protected abstract string RepositoryFolder { get; }

    protected abstract int ExpectedYamlFileCount { get; }

    protected abstract IReadOnlyCollection<GuidelineId> ExpectedGuidelineIds { get; }

    internal IReadOnlyCollection<GuidelineId> GetExpectedGuidelineIds() => ExpectedGuidelineIds;

    [Fact]
    public async Task AnalyseRepository_ShouldFindEveryExpectedGuidelineWithRepeatedDiagnostics()
    {
        using ServiceProvider provider = CreateServiceProvider();
        PipelinePathResolver pathResolver = provider.GetRequiredService<PipelinePathResolver>();
        IPipelineParser parser = provider.GetRequiredService<IPipelineParser>();
        IPipelineAnalyser analyser = provider.GetRequiredService<IPipelineAnalyser>();
        IGuidelineAutomationMetadataProvider automationMetadataProvider =
            provider.GetRequiredService<IGuidelineAutomationMetadataProvider>();
        GuidelineId[] expectedEvaluatedGuidelineIds = [.. ExpectedGuidelineIds.Where(id =>
            automationMetadataProvider.GetAutomationMetadata(id)?.Status is
                GuidelineAutomationStatus.Enforceable or GuidelineAutomationStatus.Heuristic)];

        string repositoryPath = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "PipelineRepositories",
            RepositoryFolder);

        IReadOnlyList<string> paths = pathResolver.Resolve([repositoryPath]);
        paths.Should().HaveCount(ExpectedYamlFileCount);

        List<Diagnostic> diagnostics = [];
        foreach (string path in paths)
        {
            string yaml = await File.ReadAllTextAsync(path);
            PipelineDocument document = parser.Parse(yaml, path);
            AnalysisResult result = await analyser.AnalyseAsync(
                document,
                new AnalysisOptions(
                    IncludedGuidelineIds: [.. ExpectedGuidelineIds],
                    IncludeHeuristics: true));
            diagnostics.AddRange(result.Diagnostics);
        }

        diagnostics.Select(diagnostic => diagnostic.GuidelineId)
            .Distinct()
            .Should()
            .BeEquivalentTo(expectedEvaluatedGuidelineIds);

        diagnostics.Should().HaveCountGreaterThan(expectedEvaluatedGuidelineIds.Length);
    }

    internal static ServiceProvider CreateServiceProvider()
    {
        ServiceCollection services = [];
        services.AddSingleton<IGuidelineRepository>(new GuidelineRepository([]));
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddGuidelinesAnalysis();
        return services.BuildServiceProvider();
    }
}
