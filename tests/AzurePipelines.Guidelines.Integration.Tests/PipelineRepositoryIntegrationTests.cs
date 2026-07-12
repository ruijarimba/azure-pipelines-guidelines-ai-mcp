using System.Text.Json;
using AzurePipelines.Guidelines.Analysis;
using AzurePipelines.Guidelines.Core;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AzurePipelines.Guidelines.Integration.Tests;

public sealed class PipelineRepositoryIntegrationTests
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task AnalyseCommittedRepositories_GivenExpectedResultsManifest_ShouldResolveAndAnalyseEveryFixture()
    {
        ExpectedResultsManifest manifest = await LoadManifestAsync();
        using ServiceProvider provider = CreateServiceProvider();
        PipelinePathResolver pathResolver = provider.GetRequiredService<PipelinePathResolver>();
        IPipelineParser parser = provider.GetRequiredService<IPipelineParser>();
        IPipelineAnalyser analyser = provider.GetRequiredService<IPipelineAnalyser>();

        foreach (RepositoryExpectation repository in manifest.Repositories)
        {
            string repositoryPath = Path.Combine(
                AppContext.BaseDirectory,
                "Fixtures",
                "PipelineRepositories",
                repository.PipelinePath);

            IReadOnlyList<string> paths = pathResolver.Resolve([repositoryPath]);
            paths.Should().HaveCount(repository.YamlFileCount, repository.Name);

            List<Diagnostic> diagnostics = [];
            foreach (string path in paths)
            {
                string yaml = await File.ReadAllTextAsync(path);
                PipelineDocument document = parser.Parse(yaml, path);
                AnalysisResult result = await analyser.AnalyseAsync(
                    document,
                    new AnalysisOptions(IncludedGuidelineIds: [new GuidelineId("ADOG-STEPS-006")]));
                diagnostics.AddRange(result.Diagnostics);
            }

            diagnostics.Select(diagnostic => diagnostic.GuidelineId.Value)
                .Should()
                .Contain(repository.ExpectedDiagnosticRuleIds, repository.Name);
        }
    }

    private static ServiceProvider CreateServiceProvider()
    {
        ServiceCollection services = new();
        services.AddSingleton<IGuidelineRepository>(new IntegrationGuidelineRepository());
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddGuidelinesAnalysis();
        return services.BuildServiceProvider();
    }

    private static async Task<ExpectedResultsManifest> LoadManifestAsync()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "expected-results.json");
        await using FileStream stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<ExpectedResultsManifest>(stream, _jsonOptions)
            ?? throw new InvalidOperationException("The expected-results manifest cannot be empty.");
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1812:Avoid uninstantiated internal classes",
        Justification = "Instantiated by System.Text.Json deserialization.")]
    private sealed record ExpectedResultsManifest(IReadOnlyList<RepositoryExpectation> Repositories);

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1812:Avoid uninstantiated internal classes",
        Justification = "Instantiated by System.Text.Json deserialization.")]
    private sealed record RepositoryExpectation(
        string Name,
        string PipelinePath,
        int YamlFileCount,
        IReadOnlyList<string> ExpectedDiagnosticRuleIds);

    private sealed class IntegrationGuidelineRepository : IGuidelineRepository
    {
        private static readonly IReadOnlyList<GuidelineDefinition> _definitions =
        [
            new(
                new GuidelineId("ADOG-STEPS-006"),
                GuidelineCategory.Steps,
                GuidelineSeverity.Consider,
                "Set task timeouts",
                "Task steps should declare timeouts.",
                Rationale: null,
                Tags: [],
                DetectionHints: [],
                Fix: null,
                References: [])
        ];

        public IReadOnlyList<GuidelineDefinition> GetAll() => _definitions;

        public GuidelineDefinition? FindById(GuidelineId id) =>
            _definitions.FirstOrDefault(definition => definition.Id.Equals(id));

        public IReadOnlyList<GuidelineDefinition> GetByCategory(GuidelineCategory category) =>
            _definitions.Where(definition => definition.Category == category).ToList();
    }
}
