using AzurePipelines.Guidelines.Analysis;
using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Parsing;
using AzurePipelines.Guidelines.Rules;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AzurePipelines.Guidelines.Analysis.Tests;

public sealed class FolderBasedPipelineAnalysisTests
{
    [Fact]
    public async Task AnalyseFolder_GivenCompliantFixture_ShouldReturnNoDiagnostics()
    {
        string fixtureRoot = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Integration", "TemplateRepo", "Compliant");

        IReadOnlyList<Diagnostic> diagnostics = await AnalyseFixtureAsync(fixtureRoot);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyseFolder_GivenViolatingFixture_ShouldReturnExpectedDiagnostic()
    {
        string fixtureRoot = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Integration", "TemplateRepo", "Violations");

        IReadOnlyList<Diagnostic> diagnostics = await AnalyseFixtureAsync(fixtureRoot);

        diagnostics.Should().Contain(d => d.GuidelineId.Value == "ADOG-STEPS-007");
        diagnostics.Should().Contain(d => Path.GetFileName(d.FilePath).Equals("step-with-controls.yml", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Analyses every YAML file below a fixture directory.</summary>
    /// <param name="fixtureRoot">The fixture directory to scan.</param>
    /// <returns>All diagnostics produced for the fixture files.</returns>
    private static async Task<IReadOnlyList<Diagnostic>> AnalyseFixtureAsync(string fixtureRoot)
    {
        IReadOnlyList<string> yamlFiles = Directory
            .EnumerateFiles(fixtureRoot, "*.yml", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(fixtureRoot, "*.yaml", SearchOption.AllDirectories))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        ServiceCollection services = new();
        services.AddSingleton<IPipelineParser, YamlPipelineParser>();
        services.AddSingleton<IPipelineSchemaValidator, YamlPipelineSchemaValidator>();
        services.AddGuidelineRules();
        services.AddSingleton<IPipelineAnalyser, PipelineAnalyser>();
        services.AddSingleton<PipelinePathResolver>();
        IGuidelineRepository repository = new TestGuidelineRepository();
        services.AddSingleton(repository);
        services.AddSingleton<ILogger<PipelineAnalyser>>(NullLogger<PipelineAnalyser>.Instance);
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);

        IServiceProvider provider = services.BuildServiceProvider();
        IPipelineAnalyser analyser = provider.GetRequiredService<IPipelineAnalyser>();
        IPipelineParser parser = provider.GetRequiredService<IPipelineParser>();

        List<Diagnostic> diagnostics = [];

        foreach (string yamlFile in yamlFiles)
        {
            string content = await File.ReadAllTextAsync(yamlFile);
            PipelineDocument document = parser.Parse(content, yamlFile);
            AnalysisOptions options = new(
                IncludedGuidelineIds: [new GuidelineId("ADOG-STEPS-007")]);
            AnalysisResult result = await analyser.AnalyseAsync(document, options);
            diagnostics.AddRange(result.Diagnostics);
        }

        return diagnostics;
    }

    /// <summary>Provides the single rule needed by the folder-analysis fixtures.</summary>
    private sealed class TestGuidelineRepository : IGuidelineRepository
    {
        public IReadOnlyList<GuidelineDefinition> GetAll() =>
            [
                new GuidelineDefinition(
                    new GuidelineId("ADOG-STEPS-007"),
                    GuidelineCategory.Steps,
                    GuidelineSeverity.Consider,
                    "Expose template controls as parameters",
                    "Reusable step templates should expose controls as parameters.",
                    Rationale: null,
                    Tags: [],
                    DetectionHints: [],
                    Fix: null,
                    References: [])
            ];

        public GuidelineDefinition? FindById(GuidelineId id) =>
            GetAll().FirstOrDefault(def => def.Id.Equals(id));

        public IReadOnlyList<GuidelineDefinition> GetByCategory(GuidelineCategory category) =>
            GetAll().Where(def => def.Category == category).ToList();
    }
}
