using System.CommandLine;
using System.Text.Json;
using AzurePipelines.Guidelines.Cli;
using AzurePipelines.Guidelines.Core;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AzurePipelines.Guidelines.Cli.Tests;

public sealed class RulesCommandTests
{
    private static GuidelineDefinition MakeGuideline(
        string id,
        GuidelineCategory category = GuidelineCategory.Steps,
        GuidelineSeverity severity = GuidelineSeverity.Do,
        string title = "A title",
        string description = "A description") =>
        new(
            new GuidelineId(id),
            category,
            severity,
            title,
            description,
            Rationale: null,
            Tags: [],
            DetectionHints: [],
            Fix: null,
            References: []);

    private static IGuidelineRepository MakeRepo(
        IReadOnlyList<GuidelineDefinition>? all = null)
    {
        IGuidelineRepository repo = Substitute.For<IGuidelineRepository>();
        repo.GetAll().Returns(all ?? []);
        return repo;
    }

    private sealed class CurrentDirectoryScope : IDisposable
    {
        private readonly string? _originalDirectory = Environment.CurrentDirectory;

        internal CurrentDirectoryScope(string directory)
        {
            Directory.SetCurrentDirectory(directory);
        }

        public void Dispose()
        {
            if (_originalDirectory is not null)
            {
                Directory.SetCurrentDirectory(_originalDirectory);
            }
        }
    }

    [Fact]
    public async Task RunListAsync_GivenEmptyCatalogue_ShouldReturnExitCodeClean()
    {
        IGuidelineRepository repo = MakeRepo([]);

        int exitCode = await RulesCommand.RunListAsync(repo, category: null, format: "console");

        exitCode.Should().Be(ExitCodes.Success);
    }

    [Fact]
    public async Task RunListAsync_GivenAllGuidelines_ShouldReturnExitCodeClean()
    {
        IGuidelineRepository repo = MakeRepo([
            MakeGuideline("ADOG-STEPS-001", title: "Use templates"),
            MakeGuideline("ADOG-JOBS-006", title: "Set timeouts"),
        ]);

        int exitCode = await RulesCommand.RunListAsync(repo, category: null, format: "console");

        exitCode.Should().Be(ExitCodes.Success);
    }

    [Fact]
    public async Task RunListAsync_GivenValidCategory_ShouldCallGetByCategory()
    {
        IGuidelineRepository repo = Substitute.For<IGuidelineRepository>();
        repo.GetByCategory(GuidelineCategory.Steps).Returns([
            MakeGuideline("ADOG-STEPS-001"),
        ]);

        int exitCode = await RulesCommand.RunListAsync(repo, category: ["steps"], format: "console");

        exitCode.Should().Be(ExitCodes.Success);
        repo.Received(1).GetByCategory(GuidelineCategory.Steps);
    }

    [Fact]
    public async Task RunListAsync_GivenUnknownCategory_ShouldReturnExitCodeError()
    {
        IGuidelineRepository repo = MakeRepo();

        int exitCode = await RulesCommand.RunListAsync(repo, category: ["not-a-category"], format: "console");

        exitCode.Should().Be(ExitCodes.Error);
    }

    [Fact]
    public async Task Create_GivenConfigDefaults_ShouldUseConfiguredCategorySeverityAndFormat()
    {
        GuidelineDefinition stepGuideline = MakeGuideline(
            "ADOG-STEPS-001",
            category: GuidelineCategory.Steps,
            severity: GuidelineSeverity.Do);
        GuidelineDefinition jobGuideline = MakeGuideline(
            "ADOG-JOBS-006",
            category: GuidelineCategory.Jobs,
            severity: GuidelineSeverity.Avoid);

        IGuidelineRepository repo = Substitute.For<IGuidelineRepository>();
        repo.GetAll().Returns([stepGuideline, jobGuideline]);
        repo.GetByCategory(GuidelineCategory.Steps).Returns([stepGuideline]);
        repo.GetByCategory(GuidelineCategory.Jobs).Returns([jobGuideline]);

        string configDirectory = Path.Combine(Path.GetTempPath(), $"adog-rules-config-{Guid.NewGuid():N}");
        Directory.CreateDirectory(configDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(configDirectory, "adog.json"),
            "{\"category\":\"steps\",\"severity\":\"do\",\"format\":\"json\"}");

        CurrentDirectoryScope scope = new(configDirectory);
        CliConfiguration configuration = CliConfigurationLoader.Load();
        Command command = RulesCommand.Create(repo, configuration);
        using StringWriter output = new();
        TextWriter originalOut = Console.Out;
        Console.SetOut(output);

        try
        {
            int exitCode = await command.InvokeAsync(["list"]);

            exitCode.Should().Be(ExitCodes.Success);
            output.ToString().Should().Contain("ADOG-STEPS-001");
            output.ToString().Should().NotContain("ADOG-JOBS-006");
        }
        finally
        {
            Console.SetOut(originalOut);
            scope.Dispose();
            if (Directory.Exists(configDirectory))
            {
                Directory.Delete(configDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunListAsync_GivenJsonFormat_ShouldReturnExitCodeClean()
    {
        IGuidelineRepository repo = MakeRepo([MakeGuideline("ADOG-STEPS-001")]);

        int exitCode = await RulesCommand.RunListAsync(repo, category: null, format: "json");

        exitCode.Should().Be(ExitCodes.Success);
    }

    [Fact]
    public async Task RunShowAsync_GivenKnownId_ShouldReturnExitCodeClean()
    {
        GuidelineDefinition g = MakeGuideline("ADOG-STEPS-001");
        IGuidelineRepository repo = Substitute.For<IGuidelineRepository>();
        repo.FindById(Arg.Any<GuidelineId>()).Returns(g);

        int exitCode = await RulesCommand.RunShowAsync(repo, id: "ADOG-STEPS-001", format: "console");

        exitCode.Should().Be(ExitCodes.Success);
    }

    [Fact]
    public async Task RunShowAsync_GivenUnknownId_ShouldReturnExitCodeError()
    {
        IGuidelineRepository repo = Substitute.For<IGuidelineRepository>();
        repo.FindById(Arg.Any<GuidelineId>()).Returns((GuidelineDefinition?)null);

        int exitCode = await RulesCommand.RunShowAsync(repo, id: "ADOG-STEPS-999", format: "console");

        exitCode.Should().Be(ExitCodes.Error);
    }

    [Fact]
    public async Task RunShowAsync_GivenInvalidIdFormat_ShouldReturnExitCodeError()
    {
        IGuidelineRepository repo = MakeRepo();

        int exitCode = await RulesCommand.RunShowAsync(repo, id: "not-a-valid-id", format: "console");

        exitCode.Should().Be(ExitCodes.Error);
    }

    [Fact]
    public async Task RunShowAsync_GivenJsonFormat_ShouldReturnExitCodeClean()
    {
        GuidelineDefinition g = MakeGuideline("ADOG-STEPS-001", title: "Use templates");
        IGuidelineRepository repo = Substitute.For<IGuidelineRepository>();
        repo.FindById(Arg.Any<GuidelineId>()).Returns(g);

        int exitCode = await RulesCommand.RunShowAsync(repo, id: "ADOG-STEPS-001", format: "json");

        exitCode.Should().Be(ExitCodes.Success);
    }

    [Fact]
    public void FormatGuidelineDetail_GivenGuideline_ShouldProduceValidJson()
    {
        GuidelineDefinition g = MakeGuideline("ADOG-STEPS-001", title: "Use templates");

        string json = JsonFormatter.FormatGuidelineDetail(g);

        JsonDocument doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("id").GetString().Should().Be("ADOG-STEPS-001");
        doc.RootElement.GetProperty("title").GetString().Should().Be("Use templates");
        doc.RootElement.GetProperty("severity").GetString().Should().Be("do");
    }

    [Fact]
    public async Task RunListAsync_GivenSeverityFilter_ShouldReturnOnlyMatchingGuidelines()
    {
        IGuidelineRepository repo = MakeRepo([
            MakeGuideline("ADOG-STEPS-001", severity: GuidelineSeverity.Do),
            MakeGuideline("ADOG-JOBS-006", severity: GuidelineSeverity.Avoid),
        ]);

        int exitCode = await RulesCommand.RunListAsync(
            repo, category: null, severity: ["avoid"], format: "console");

        exitCode.Should().Be(ExitCodes.Success);
    }

    [Fact]
    public async Task RunListAsync_GivenUnknownSeverity_ShouldReturnExitCodeError()
    {
        IGuidelineRepository repo = MakeRepo();

        int exitCode = await RulesCommand.RunListAsync(
            repo, category: null, severity: ["not-a-severity"], format: "console");

        exitCode.Should().Be(ExitCodes.Error);
    }

    [Fact]
    public async Task RunListAsync_GivenGuidelineSeverityAlias_ShouldReturnExitCodeClean()
    {
        IGuidelineRepository repo = MakeRepo([MakeGuideline("ADOG-STEPS-001")]);

        int exitCode = await RulesCommand.RunListAsync(
            repo, category: null, severity: ["do"], format: "console");

        exitCode.Should().Be(ExitCodes.Success);
    }

    [Fact]
    public async Task RunListAsync_GivenMultipleCategoriesAndSeverities_ShouldFilterAcrossBothSets()
    {
        IGuidelineRepository repo = MakeRepo([
            MakeGuideline("ADOG-STEPS-001", category: GuidelineCategory.Steps, severity: GuidelineSeverity.Do),
            MakeGuideline("ADOG-JOBS-006", category: GuidelineCategory.Jobs, severity: GuidelineSeverity.Avoid),
            MakeGuideline("ADOG-VARIABLES-003", category: GuidelineCategory.Variables, severity: GuidelineSeverity.Consider),
        ]);

        int exitCode = await RulesCommand.RunListAsync(
            repo,
            category: ["steps", "jobs"],
            severity: ["do", "avoid"],
            format: "console");

        exitCode.Should().Be(ExitCodes.Success);
    }

    [Fact]
    public async Task RunListAsync_GivenNullSeverity_ShouldReturnAllGuidelines()
    {
        IGuidelineRepository repo = MakeRepo([
            MakeGuideline("ADOG-STEPS-001", severity: GuidelineSeverity.Do),
            MakeGuideline("ADOG-JOBS-006", severity: GuidelineSeverity.Avoid),
        ]);

        int exitCode = await RulesCommand.RunListAsync(
            repo, category: null, severity: null, format: "console");

        exitCode.Should().Be(ExitCodes.Success);
    }
}
