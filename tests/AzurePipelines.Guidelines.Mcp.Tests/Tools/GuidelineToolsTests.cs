using System.Text.Json;
using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Mcp.Tools;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AzurePipelines.Guidelines.Mcp.Tests.Tools;

public sealed class GuidelineToolsTests
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

    private static GuidelineTools MakeSut(IReadOnlyList<GuidelineDefinition>? all = null)
    {
        IGuidelineRepository repo = Substitute.For<IGuidelineRepository>();
        repo.GetAll().Returns(all ?? []);
        return new GuidelineTools(repo, AutomationMetadataProvider());
    }

    private static GuidelineTools MakeSutWithRepo(IGuidelineRepository repo) =>
        new(repo, AutomationMetadataProvider());

    private static IGuidelineAutomationMetadataProvider AutomationMetadataProvider()
    {
        IGuidelineAutomationMetadataProvider provider = Substitute.For<IGuidelineAutomationMetadataProvider>();
        provider.GetAutomationMetadata(Arg.Any<GuidelineId>())
            .Returns(new GuidelineAutomationMetadata(
                GuidelineAutomationStatus.Enforceable,
                "Test automation metadata."));
        return provider;
    }

    private static T Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json)!;

    // ── ListGuidelines ────────────────────────────────────────────────────────

    [Fact]
    public void ListGuidelines_GivenEmptyRepository_ShouldReturnEmptyArray()
    {
        // Arrange
        GuidelineTools sut = MakeSut([]);

        // Act
        string result = sut.ListGuidelines();

        // Assert
        JsonElement[] items = Deserialize<JsonElement[]>(result);
        items.Should().BeEmpty();
    }

    [Fact]
    public void ListGuidelines_GivenTwoGuidelines_ShouldReturnBothSummaries()
    {
        // Arrange
        GuidelineTools sut = MakeSut([
            MakeGuideline("ADOG-STEPS-001", title: "First"),
            MakeGuideline("ADOG-STEPS-002", title: "Second"),
        ]);

        // Act
        string result = sut.ListGuidelines();

        // Assert
        JsonElement[] items = Deserialize<JsonElement[]>(result);
        items.Should().HaveCount(2);
        items[0].GetProperty("id").GetString().Should().Be("ADOG-STEPS-001");
        items[1].GetProperty("id").GetString().Should().Be("ADOG-STEPS-002");
    }

    [Fact]
    public void ListGuidelines_ShouldIncludeRequiredFields()
    {
        // Arrange
        GuidelineTools sut = MakeSut([
            MakeGuideline("ADOG-STEPS-001", GuidelineCategory.Steps, GuidelineSeverity.Do, "My title"),
        ]);

        // Act
        string result = sut.ListGuidelines();

        // Assert
        JsonElement[] items = Deserialize<JsonElement[]>(result);
        JsonElement item = items[0];
        item.GetProperty("id").GetString().Should().Be("ADOG-STEPS-001");
        item.GetProperty("title").GetString().Should().Be("My title");
        item.GetProperty("category").GetString().Should().Be("steps");
        item.GetProperty("severity").GetString().Should().Be("do");
        item.GetProperty("automationStatus").GetString().Should().Be("enforceable");
    }

    [Fact]
    public void ListGuidelines_GivenValidCategoryFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        IGuidelineRepository repo = Substitute.For<IGuidelineRepository>();
        GuidelineDefinition stepsGuideline = MakeGuideline("ADOG-STEPS-001", GuidelineCategory.Steps);
        repo.GetByCategory(GuidelineCategory.Steps).Returns([stepsGuideline]);
        GuidelineTools sut = MakeSutWithRepo(repo);

        // Act
        string result = sut.ListGuidelines("steps");

        // Assert
        JsonElement[] items = Deserialize<JsonElement[]>(result);
        items.Should().HaveCount(1);
        items[0].GetProperty("id").GetString().Should().Be("ADOG-STEPS-001");
    }

    [Fact]
    public void ListGuidelines_GivenUnknownCategory_ShouldReturnErrorObject()
    {
        // Arrange
        GuidelineTools sut = MakeSut();

        // Act
        string result = sut.ListGuidelines("not-a-real-category");

        // Assert
        JsonElement obj = Deserialize<JsonElement>(result);
        obj.GetProperty("error").GetString().Should().Contain("not-a-real-category");
    }

    [Theory]
    [InlineData("general", GuidelineCategory.General)]
    [InlineData("jobs", GuidelineCategory.Jobs)]
    [InlineData("parameters", GuidelineCategory.Parameters)]
    [InlineData("pipelines", GuidelineCategory.Pipelines)]
    [InlineData("stages", GuidelineCategory.Stages)]
    [InlineData("variables", GuidelineCategory.Variables)]
    public void ListGuidelines_GivenAllKnownCategories_ShouldCallRepositoryWithCorrectCategory(
        string categoryFilter, GuidelineCategory expectedEnum)
    {
        // Arrange
        IGuidelineRepository repo = Substitute.For<IGuidelineRepository>();
        repo.GetByCategory(Arg.Any<GuidelineCategory>()).Returns([]);
        GuidelineTools sut = MakeSutWithRepo(repo);

        // Act
        _ = sut.ListGuidelines(categoryFilter);

        // Assert
        repo.Received(1).GetByCategory(expectedEnum);
    }

    // ── GetGuideline ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GetGuideline_GivenNullOrWhitespaceId_ShouldReturnErrorObject(string id)
    {
        // Arrange
        GuidelineTools sut = MakeSut();

        // Act
        string result = sut.GetGuideline(id);

        // Assert
        JsonElement obj = Deserialize<JsonElement>(result);
        obj.GetProperty("error").GetString().Should().Contain("required");
    }

    [Fact]
    public void GetGuideline_GivenInvalidIdFormat_ShouldReturnErrorObject()
    {
        // Arrange
        GuidelineTools sut = MakeSut();

        // Act
        string result = sut.GetGuideline("INVALID-ID");

        // Assert
        JsonElement obj = Deserialize<JsonElement>(result);
        obj.GetProperty("error").GetString().Should().Contain("INVALID-ID");
    }

    [Fact]
    public void GetGuideline_GivenUnknownId_ShouldReturnErrorObject()
    {
        // Arrange
        IGuidelineRepository repo = Substitute.For<IGuidelineRepository>();
        repo.FindById(Arg.Any<GuidelineId>()).Returns((GuidelineDefinition?)null);
        GuidelineTools sut = MakeSutWithRepo(repo);

        // Act
        string result = sut.GetGuideline("ADOG-STEPS-099");

        // Assert
        JsonElement obj = Deserialize<JsonElement>(result);
        obj.GetProperty("error").GetString().Should().Contain("ADOG-STEPS-099");
    }

    [Fact]
    public void GetGuideline_GivenKnownId_ShouldReturnFullDetails()
    {
        // Arrange
        GuidelineDefinition guideline = MakeGuideline(
            "ADOG-STEPS-001",
            GuidelineCategory.Steps,
            GuidelineSeverity.Avoid,
            "My title",
            "My description");

        IGuidelineRepository repo = Substitute.For<IGuidelineRepository>();
        repo.FindById(Arg.Is<GuidelineId>(x => x.Value == "ADOG-STEPS-001")).Returns(guideline);
        GuidelineTools sut = MakeSutWithRepo(repo);

        // Act
        string result = sut.GetGuideline("ADOG-STEPS-001");

        // Assert
        JsonElement obj = Deserialize<JsonElement>(result);
        obj.GetProperty("id").GetString().Should().Be("ADOG-STEPS-001");
        obj.GetProperty("title").GetString().Should().Be("My title");
        obj.GetProperty("description").GetString().Should().Be("My description");
        obj.GetProperty("category").GetString().Should().Be("steps");
        obj.GetProperty("severity").GetString().Should().Be("avoid");
    }

    [Fact]
    public void GetGuideline_GivenGuidelineWithFix_ShouldIncludeFixInResponse()
    {
        // Arrange
        GuidelineDefinition guideline = new(
            new GuidelineId("ADOG-STEPS-001"),
            GuidelineCategory.Steps,
            GuidelineSeverity.Do,
            "Title",
            "Desc",
            Rationale: null,
            Tags: [],
            DetectionHints: [],
            Fix: new FixGuidance("Do this instead.", Before: "bad", After: "good"),
            References: []);

        IGuidelineRepository repo = Substitute.For<IGuidelineRepository>();
        repo.FindById(Arg.Any<GuidelineId>()).Returns(guideline);
        GuidelineTools sut = MakeSutWithRepo(repo);

        // Act
        string result = sut.GetGuideline("ADOG-STEPS-001");

        // Assert
        JsonElement obj = Deserialize<JsonElement>(result);
        JsonElement fix = obj.GetProperty("fix");
        fix.GetProperty("summary").GetString().Should().Be("Do this instead.");
        fix.GetProperty("before").GetString().Should().Be("bad");
        fix.GetProperty("after").GetString().Should().Be("good");
    }

    [Fact]
    public void GetGuideline_GivenGuidelineWithDetectionHints_ShouldIncludeHintsInResponse()
    {
        // Arrange
        GuidelineDefinition guideline = new(
            new GuidelineId("ADOG-STEPS-001"),
            GuidelineCategory.Steps,
            GuidelineSeverity.Do,
            "Title",
            "Desc",
            Rationale: null,
            Tags: [],
            DetectionHints:
            [
                new DetectionHint(DetectionKind.Regex, PipelineScope.Step, @"task:\s*", "Matches task steps"),
            ],
            Fix: null,
            References: []);

        IGuidelineRepository repo = Substitute.For<IGuidelineRepository>();
        repo.FindById(Arg.Any<GuidelineId>()).Returns(guideline);
        GuidelineTools sut = MakeSutWithRepo(repo);

        // Act
        string result = sut.GetGuideline("ADOG-STEPS-001");

        // Assert
        JsonElement obj = Deserialize<JsonElement>(result);
        JsonElement hints = obj.GetProperty("detectionHints");
        hints.GetArrayLength().Should().Be(1);
        hints[0].GetProperty("kind").GetString().Should().Be("regex");
        hints[0].GetProperty("scope").GetString().Should().Be("step");
        hints[0].GetProperty("expression").GetString().Should().Be(@"task:\s*");
        hints[0].GetProperty("description").GetString().Should().Be("Matches task steps");
    }

    // ── SearchGuidelines ──────────────────────────────────────────────────────

    [Fact]
    public void SearchGuidelines_GivenEmptyKeyword_ShouldReturnErrorObject()
    {
        // Arrange
        GuidelineTools sut = MakeSut();

        // Act
        string result = sut.SearchGuidelines(string.Empty);

        // Assert
        JsonElement obj = Deserialize<JsonElement>(result);
        obj.GetProperty("error").GetString().Should().Contain("required");
    }

    [Fact]
    public void SearchGuidelines_GivenKeywordMatchingTitle_ShouldReturnMatchingGuidelines()
    {
        // Arrange
        GuidelineTools sut = MakeSut([
            MakeGuideline("ADOG-STEPS-001", title: "Use inline scripts"),
            MakeGuideline("ADOG-STEPS-002", title: "Prefer template references"),
        ]);

        // Act
        string result = sut.SearchGuidelines("inline");

        // Assert
        JsonElement[] items = Deserialize<JsonElement[]>(result);
        items.Should().HaveCount(1);
        items[0].GetProperty("id").GetString().Should().Be("ADOG-STEPS-001");
    }

    [Fact]
    public void SearchGuidelines_GivenKeywordMatchingDescription_ShouldReturnMatchingGuidelines()
    {
        // Arrange
        GuidelineTools sut = MakeSut([
            MakeGuideline("ADOG-STEPS-001", description: "Avoid using PowerShell inline."),
            MakeGuideline("ADOG-STEPS-002", description: "Always pin task versions."),
        ]);

        // Act
        string result = sut.SearchGuidelines("PowerShell");

        // Assert
        JsonElement[] items = Deserialize<JsonElement[]>(result);
        items.Should().HaveCount(1);
        items[0].GetProperty("id").GetString().Should().Be("ADOG-STEPS-001");
    }

    [Fact]
    public void SearchGuidelines_GivenCaseInsensitiveKeyword_ShouldReturnMatches()
    {
        // Arrange
        GuidelineTools sut = MakeSut([
            MakeGuideline("ADOG-STEPS-001", title: "USE TEMPLATES"),
        ]);

        // Act
        string result = sut.SearchGuidelines("templates");

        // Assert
        JsonElement[] items = Deserialize<JsonElement[]>(result);
        items.Should().HaveCount(1);
    }

    [Fact]
    public void SearchGuidelines_GivenNoMatches_ShouldReturnEmptyArray()
    {
        // Arrange
        GuidelineTools sut = MakeSut([
            MakeGuideline("ADOG-STEPS-001", title: "Some title"),
        ]);

        // Act
        string result = sut.SearchGuidelines("xyzzy-no-match");

        // Assert
        JsonElement[] items = Deserialize<JsonElement[]>(result);
        items.Should().BeEmpty();
    }

    // ── ListCategories ────────────────────────────────────────────────────────

    [Fact]
    public void ListCategories_GivenEmptyRepository_ShouldReturnEmptyArray()
    {
        // Arrange
        GuidelineTools sut = MakeSut([]);

        // Act
        string result = sut.ListCategories();

        // Assert
        JsonElement[] items = Deserialize<JsonElement[]>(result);
        items.Should().BeEmpty();
    }

    [Fact]
    public void ListCategories_GivenGuidelinesFromTwoCategories_ShouldReturnBothCounts()
    {
        // Arrange
        GuidelineTools sut = MakeSut([
            MakeGuideline("ADOG-STEPS-001", GuidelineCategory.Steps),
            MakeGuideline("ADOG-STEPS-002", GuidelineCategory.Steps),
            MakeGuideline("ADOG-JOBS-001", GuidelineCategory.Jobs),
        ]);

        // Act
        string result = sut.ListCategories();

        // Assert
        JsonElement[] items = Deserialize<JsonElement[]>(result);
        items.Should().HaveCount(2);

        JsonElement jobs = items.First(i => i.GetProperty("category").GetString() == "jobs");
        JsonElement steps = items.First(i => i.GetProperty("category").GetString() == "steps");
        jobs.GetProperty("count").GetInt32().Should().Be(1);
        steps.GetProperty("count").GetInt32().Should().Be(2);
    }

    [Fact]
    public void ListCategories_ShouldReturnResultsSortedAlphabetically()
    {
        // Arrange
        GuidelineTools sut = MakeSut([
            MakeGuideline("ADOG-VARIABLES-001", GuidelineCategory.Variables),
            MakeGuideline("ADOG-JOBS-001", GuidelineCategory.Jobs),
            MakeGuideline("ADOG-STAGES-001", GuidelineCategory.Stages),
        ]);

        // Act
        string result = sut.ListCategories();

        // Assert
        JsonElement[] items = Deserialize<JsonElement[]>(result);
        string[] categories = items
            .Select(i => i.GetProperty("category").GetString()!)
            .ToArray();
        categories.Should().BeInAscendingOrder();
    }
}
