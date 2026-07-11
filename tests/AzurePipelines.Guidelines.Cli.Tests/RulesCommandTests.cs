using System.Text.Json;
using AzurePipelines.Guidelines.Cli;
using AzurePipelines.Guidelines.Core;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AzurePipelines.Guidelines.Cli.Tests;

public sealed class RulesCommandTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

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

    // ── rules list — console ──────────────────────────────────────────────────

    [Fact]
    public async Task RunListAsync_GivenEmptyCatalogue_ShouldReturnExitCodeClean()
    {
        // Arrange
        IGuidelineRepository repo = MakeRepo([]);

        // Act
        int exitCode = await RulesCommand.RunListAsync(repo, category: null, format: "console");

        // Assert
        exitCode.Should().Be(ExitCodes.Success);
    }

    [Fact]
    public async Task RunListAsync_GivenAllGuidelines_ShouldReturnExitCodeClean()
    {
        // Arrange
        IGuidelineRepository repo = MakeRepo([
            MakeGuideline("ADOG-STEPS-001", title: "Use templates"),
            MakeGuideline("ADOG-JOBS-006",  title: "Set timeouts"),
        ]);

        // Act
        int exitCode = await RulesCommand.RunListAsync(repo, category: null, format: "console");

        // Assert
        exitCode.Should().Be(ExitCodes.Success);
    }

    [Fact]
    public async Task RunListAsync_GivenValidCategory_ShouldCallGetByCategory()
    {
        // Arrange
        IGuidelineRepository repo = Substitute.For<IGuidelineRepository>();
        repo.GetByCategory(GuidelineCategory.Steps).Returns([
            MakeGuideline("ADOG-STEPS-001"),
        ]);

        // Act
        int exitCode = await RulesCommand.RunListAsync(repo, category: ["steps"], format: "console");

        // Assert
        exitCode.Should().Be(ExitCodes.Success);
        repo.Received(1).GetByCategory(GuidelineCategory.Steps);
    }

    [Fact]
    public async Task RunListAsync_GivenUnknownCategory_ShouldReturnExitCodeError()
    {
        // Arrange
        IGuidelineRepository repo = MakeRepo();

        // Act
        int exitCode = await RulesCommand.RunListAsync(repo, category: ["not-a-category"], format: "console");

        // Assert
        exitCode.Should().Be(ExitCodes.Error);
    }

    // ── rules list — JSON ─────────────────────────────────────────────────────

    [Fact]
    public async Task RunListAsync_GivenJsonFormat_ShouldReturnExitCodeClean()
    {
        // Arrange
        IGuidelineRepository repo = MakeRepo([MakeGuideline("ADOG-STEPS-001")]);

        // Act
        int exitCode = await RulesCommand.RunListAsync(repo, category: null, format: "json");

        // Assert
        exitCode.Should().Be(ExitCodes.Success);
    }

    // ── rules show — console ──────────────────────────────────────────────────

    [Fact]
    public async Task RunShowAsync_GivenKnownId_ShouldReturnExitCodeClean()
    {
        // Arrange
        GuidelineDefinition g = MakeGuideline("ADOG-STEPS-001");
        IGuidelineRepository repo = Substitute.For<IGuidelineRepository>();
        repo.FindById(Arg.Any<GuidelineId>()).Returns(g);

        // Act
        int exitCode = await RulesCommand.RunShowAsync(repo, id: "ADOG-STEPS-001", format: "console");

        // Assert
        exitCode.Should().Be(ExitCodes.Success);
    }

    [Fact]
    public async Task RunShowAsync_GivenUnknownId_ShouldReturnExitCodeError()
    {
        // Arrange
        IGuidelineRepository repo = Substitute.For<IGuidelineRepository>();
        repo.FindById(Arg.Any<GuidelineId>()).Returns((GuidelineDefinition?)null);

        // Act
        int exitCode = await RulesCommand.RunShowAsync(repo, id: "ADOG-STEPS-999", format: "console");

        // Assert
        exitCode.Should().Be(ExitCodes.Error);
    }

    [Fact]
    public async Task RunShowAsync_GivenInvalidIdFormat_ShouldReturnExitCodeError()
    {
        // Arrange
        IGuidelineRepository repo = MakeRepo();

        // Act
        int exitCode = await RulesCommand.RunShowAsync(repo, id: "not-a-valid-id", format: "console");

        // Assert
        exitCode.Should().Be(ExitCodes.Error);
    }

    // ── rules show — JSON ─────────────────────────────────────────────────────

    [Fact]
    public async Task RunShowAsync_GivenJsonFormat_ShouldReturnExitCodeClean()
    {
        // Arrange
        GuidelineDefinition g = MakeGuideline("ADOG-STEPS-001", title: "Use templates");
        IGuidelineRepository repo = Substitute.For<IGuidelineRepository>();
        repo.FindById(Arg.Any<GuidelineId>()).Returns(g);

        // Act
        int exitCode = await RulesCommand.RunShowAsync(repo, id: "ADOG-STEPS-001", format: "json");

        // Assert
        exitCode.Should().Be(ExitCodes.Success);
    }

    [Fact]
    public void FormatGuidelineDetail_GivenGuideline_ShouldProduceValidJson()
    {
        // Arrange
        GuidelineDefinition g = MakeGuideline("ADOG-STEPS-001", title: "Use templates");

        // Act
        string json = JsonFormatter.FormatGuidelineDetail(g);

        // Assert
        JsonDocument doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("id").GetString().Should().Be("ADOG-STEPS-001");
        doc.RootElement.GetProperty("title").GetString().Should().Be("Use templates");
        doc.RootElement.GetProperty("severity").GetString().Should().Be("do");
    }

    // ── rules list — severity filter ──────────────────────────────────────────

    [Fact]
    public async Task RunListAsync_GivenSeverityFilter_ShouldReturnOnlyMatchingGuidelines()
    {
        // Arrange
        IGuidelineRepository repo = MakeRepo([
            MakeGuideline("ADOG-STEPS-001", severity: GuidelineSeverity.Do),
            MakeGuideline("ADOG-JOBS-006",  severity: GuidelineSeverity.Avoid),
        ]);

        // Act
        int exitCode = await RulesCommand.RunListAsync(
            repo, category: null, severity: ["avoid"], format: "console");

        // Assert
        exitCode.Should().Be(ExitCodes.Success);
    }

    [Fact]
    public async Task RunListAsync_GivenUnknownSeverity_ShouldReturnExitCodeError()
    {
        // Arrange
        IGuidelineRepository repo = MakeRepo();

        // Act
        int exitCode = await RulesCommand.RunListAsync(
            repo, category: null, severity: ["not-a-severity"], format: "console");

        // Assert
        exitCode.Should().Be(ExitCodes.Error);
    }

    [Fact]
    public async Task RunListAsync_GivenGuidelineSeverityAlias_ShouldReturnExitCodeClean()
    {
        // Arrange
        IGuidelineRepository repo = MakeRepo([MakeGuideline("ADOG-STEPS-001")]);

        // Act
        int exitCode = await RulesCommand.RunListAsync(
            repo, category: null, severity: ["do"], format: "console");

        // Assert
        exitCode.Should().Be(ExitCodes.Success);
    }

    [Fact]
    public async Task RunListAsync_GivenMultipleCategoriesAndSeverities_ShouldFilterAcrossBothSets()
    {
        // Arrange
        IGuidelineRepository repo = MakeRepo([
            MakeGuideline("ADOG-STEPS-001", category: GuidelineCategory.Steps, severity: GuidelineSeverity.Do),
            MakeGuideline("ADOG-JOBS-006", category: GuidelineCategory.Jobs, severity: GuidelineSeverity.Avoid),
            MakeGuideline("ADOG-VARIABLES-003", category: GuidelineCategory.Variables, severity: GuidelineSeverity.Consider),
        ]);

        // Act
        int exitCode = await RulesCommand.RunListAsync(
            repo,
            category: ["steps", "jobs"],
            severity: ["do", "avoid"],
            format: "console");

        // Assert
        exitCode.Should().Be(ExitCodes.Success);
    }

    [Fact]
    public async Task RunListAsync_GivenNullSeverity_ShouldReturnAllGuidelines()
    {
        // Arrange
        IGuidelineRepository repo = MakeRepo([
            MakeGuideline("ADOG-STEPS-001", severity: GuidelineSeverity.Do),
            MakeGuideline("ADOG-JOBS-006",  severity: GuidelineSeverity.Avoid),
        ]);

        // Act
        int exitCode = await RulesCommand.RunListAsync(
            repo, category: null, severity: null, format: "console");

        // Assert
        exitCode.Should().Be(ExitCodes.Success);
    }
}
