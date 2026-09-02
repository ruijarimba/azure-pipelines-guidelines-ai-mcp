using System.Text.Json;
using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Mcp.Resources;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AzurePipelines.Guidelines.Mcp.Tests.Resources;

public sealed class GuidelineResourcesTests
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

    private static GuidelineResources MakeSut(IReadOnlyList<GuidelineDefinition>? all = null)
    {
        IGuidelineRepository repo = Substitute.For<IGuidelineRepository>();
        repo.GetAll().Returns(all ?? []);
        return new GuidelineResources(repo);
    }

    private static GuidelineResources MakeSutWithRepo(IGuidelineRepository repo) =>
        new(repo);

    private static GuidelineResources MakeSutWithRepoAndMetadata(
        IGuidelineRepository repo,
        IGuidelineAutomationMetadataProvider metadataProvider) =>
        new(repo, metadataProvider);

    private static T Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json)!;

    // ── GetAllGuidelinesAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllGuidelinesAsync_GivenEmptyRepository_ShouldReturnEmptyArray()
    {
        // Arrange
        GuidelineResources sut = MakeSut([]);

        // Act
        string result = await sut.GetAllGuidelinesAsync();

        // Assert
        JsonElement[] items = Deserialize<JsonElement[]>(result);
        items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCatalogueVersionAsync_ShouldReturnRepositoryVersion()
    {
        // Arrange
        IGuidelineRepository repo = Substitute.For<IGuidelineRepository>();
        repo.ContentVersion.Returns("abc123");
        GuidelineResources sut = MakeSutWithRepo(repo);

        // Act
        string result = await sut.GetCatalogueVersionAsync();

        // Assert
        JsonElement payload = Deserialize<JsonElement>(result);
        payload.GetProperty("version").GetString().Should().Be("abc123");
    }

    [Fact]
    public async Task GetCapabilitiesAsync_ShouldReturnCurrentServerSurface()
    {
        // Arrange
        IGuidelineRepository repo = Substitute.For<IGuidelineRepository>();
        repo.ContentVersion.Returns("abc123");
        GuidelineResources sut = MakeSutWithRepo(repo);

        // Act
        string result = await sut.GetCapabilitiesAsync();

        // Assert
        JsonElement payload = Deserialize<JsonElement>(result);
        payload.GetProperty("server").GetString().Should().Be("azure-pipelines-guidelines");
        payload.GetProperty("version").GetString().Should().Be("1.0.0");
        payload.GetProperty("catalogueVersion").GetString().Should().Be("abc123");
        payload.GetProperty("tools").EnumerateArray().Select(item => item.GetString())
            .Should().Contain("analyze_template_or_folder");
        payload.GetProperty("tools").EnumerateArray().Select(item => item.GetString())
            .Should().Contain("explain_diagnostic");
        payload.GetProperty("resources").EnumerateArray().Select(item => item.GetString())
            .Should().Contain("adog://capabilities");
        payload.GetProperty("resources").EnumerateArray().Select(item => item.GetString())
            .Should().Contain("adog://guidelines/{id}/automation");
        payload.GetProperty("prompts").EnumerateArray().Select(item => item.GetString())
            .Should().Contain(["review", "review-summary", "review-category", "review-guideline"]);
        payload.GetProperty("supports").GetProperty("automationMetadata").GetBoolean().Should().BeTrue();
        payload.GetProperty("supports").GetProperty("prompts").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetGuidelinesByCategoryAsync_GivenKnownCategory_ShouldReturnFilteredSummaries()
    {
        // Arrange
        IGuidelineRepository repo = Substitute.For<IGuidelineRepository>();
        repo.GetByCategory(GuidelineCategory.Steps).Returns([
            MakeGuideline("ADOG-STEPS-001", GuidelineCategory.Steps),
        ]);
        GuidelineResources sut = MakeSutWithRepo(repo);

        // Act
        string result = await sut.GetGuidelinesByCategoryAsync("steps");

        // Assert
        JsonElement[] items = Deserialize<JsonElement[]>(result);
        items.Should().HaveCount(1);
        items[0].GetProperty("id").GetString().Should().Be("ADOG-STEPS-001");
    }

    [Fact]
    public async Task GetGuidelinesByCategoryAsync_GivenUnknownCategory_ShouldReturnErrorResponse()
    {
        // Arrange
        GuidelineResources sut = MakeSut();

        // Act
        string result = await sut.GetGuidelinesByCategoryAsync("not-a-category");

        // Assert
        JsonElement payload = Deserialize<JsonElement>(result);
        payload.GetProperty("error").GetString().Should().Contain("Unknown category");
    }

    [Fact]
    public async Task GetAllGuidelinesAsync_GivenMultipleGuidelines_ShouldReturnAllSummaries()
    {
        // Arrange
        GuidelineResources sut = MakeSut(
        [
            MakeGuideline("ADOG-STEPS-001", GuidelineCategory.Steps, GuidelineSeverity.Do, "Title A"),
            MakeGuideline("ADOG-JOBS-001", GuidelineCategory.Jobs, GuidelineSeverity.Avoid, "Title B"),
        ]);

        // Act
        string result = await sut.GetAllGuidelinesAsync();

        // Assert
        JsonElement[] items = Deserialize<JsonElement[]>(result);
        items.Should().HaveCount(2);
        items[0].GetProperty("id").GetString().Should().Be("ADOG-STEPS-001");
        items[0].GetProperty("title").GetString().Should().Be("Title A");
        items[0].GetProperty("category").GetString().Should().Be("steps");
        items[0].GetProperty("severity").GetString().Should().Be("do");
        items[1].GetProperty("id").GetString().Should().Be("ADOG-JOBS-001");
        items[1].GetProperty("severity").GetString().Should().Be("avoid");
    }

    [Fact]
    public async Task GetAllGuidelinesAsync_ShouldSerializeCategoryAndSeverityAsLowercase()
    {
        // Arrange
        GuidelineResources sut = MakeSut(
        [
            MakeGuideline("ADOG-VARIABLES-003", GuidelineCategory.Variables, GuidelineSeverity.DoNot),
        ]);

        // Act
        string result = await sut.GetAllGuidelinesAsync();

        // Assert
        JsonElement[] items = Deserialize<JsonElement[]>(result);
        items[0].GetProperty("category").GetString().Should().Be("variables");
        items[0].GetProperty("severity").GetString().Should().Be("donot");
    }

    // ── GetGuidelineAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetGuidelineAsync_GivenNullOrEmptyId_ShouldReturnErrorResponse()
    {
        // Arrange
        GuidelineResources sut = MakeSut();

        // Act
        string result = await sut.GetGuidelineAsync(string.Empty);

        // Assert
        JsonElement doc = Deserialize<JsonElement>(result);
        doc.GetProperty("error").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetGuidelineAsync_GivenInvalidIdFormat_ShouldReturnErrorResponse()
    {
        // Arrange
        GuidelineResources sut = MakeSut();

        // Act
        string result = await sut.GetGuidelineAsync("not-a-valid-id");

        // Assert
        JsonElement doc = Deserialize<JsonElement>(result);
        doc.GetProperty("error").GetString().Should().Contain("not a valid guideline ID");
    }

    [Fact]
    public async Task GetGuidelineAsync_GivenUnknownId_ShouldReturnErrorResponse()
    {
        // Arrange
        IGuidelineRepository repo = Substitute.For<IGuidelineRepository>();
        repo.FindById(Arg.Any<GuidelineId>()).Returns((GuidelineDefinition?)null);
        GuidelineResources sut = MakeSutWithRepo(repo);

        // Act
        string result = await sut.GetGuidelineAsync("ADOG-STEPS-001");

        // Assert
        JsonElement doc = Deserialize<JsonElement>(result);
        doc.GetProperty("error").GetString().Should().Contain("not found");
    }

    [Fact]
    public async Task GetGuidelineAsync_GivenKnownId_ShouldReturnGuidelineDetail()
    {
        // Arrange
        GuidelineDefinition guideline = MakeGuideline(
            "ADOG-STEPS-006",
            GuidelineCategory.Steps,
            GuidelineSeverity.Do,
            "Set a timeout",
            "Always set a timeout on tasks.");

        IGuidelineRepository repo = Substitute.For<IGuidelineRepository>();
        repo.FindById(Arg.Any<GuidelineId>()).Returns(guideline);
        GuidelineResources sut = MakeSutWithRepo(repo);

        // Act
        string result = await sut.GetGuidelineAsync("ADOG-STEPS-006");

        // Assert
        JsonElement doc = Deserialize<JsonElement>(result);
        doc.GetProperty("id").GetString().Should().Be("ADOG-STEPS-006");
        doc.GetProperty("title").GetString().Should().Be("Set a timeout");
        doc.GetProperty("description").GetString().Should().Be("Always set a timeout on tasks.");
        doc.GetProperty("category").GetString().Should().Be("steps");
        doc.GetProperty("severity").GetString().Should().Be("do");
    }

    [Fact]
    public async Task GetGuidelineAsync_GivenMetadata_ShouldIncludeAutomationStatusAndReason()
    {
        // Arrange
        GuidelineDefinition guideline = MakeGuideline("ADOG-STEPS-006");
        IGuidelineRepository repo = Substitute.For<IGuidelineRepository>();
        repo.FindById(Arg.Any<GuidelineId>()).Returns(guideline);
        IGuidelineAutomationMetadataProvider metadataProvider = Substitute.For<IGuidelineAutomationMetadataProvider>();
        metadataProvider.GetAutomationMetadata(guideline.Id).Returns(
            new GuidelineAutomationMetadata(GuidelineAutomationStatus.Enforceable, "The YAML evidence is deterministic."));
        GuidelineResources sut = MakeSutWithRepoAndMetadata(repo, metadataProvider);

        // Act
        string result = await sut.GetGuidelineAsync("ADOG-STEPS-006");

        // Assert
        JsonElement doc = Deserialize<JsonElement>(result);
        doc.GetProperty("automationStatus").GetString().Should().Be("enforceable");
        doc.GetProperty("automationReason").GetString().Should().Be("The YAML evidence is deterministic.");
    }

    [Fact]
    public async Task GetGuidelineAutomationAsync_GivenKnownId_ShouldReturnMetadata()
    {
        // Arrange
        GuidelineDefinition guideline = MakeGuideline("ADOG-STEPS-006");
        IGuidelineRepository repo = Substitute.For<IGuidelineRepository>();
        repo.FindById(Arg.Any<GuidelineId>()).Returns(guideline);
        IGuidelineAutomationMetadataProvider metadataProvider = Substitute.For<IGuidelineAutomationMetadataProvider>();
        metadataProvider.GetAutomationMetadata(guideline.Id).Returns(
            new GuidelineAutomationMetadata(GuidelineAutomationStatus.Heuristic, "The YAML evidence needs context."));
        GuidelineResources sut = MakeSutWithRepoAndMetadata(repo, metadataProvider);

        // Act
        string result = await sut.GetGuidelineAutomationAsync("ADOG-STEPS-006");

        // Assert
        JsonElement doc = Deserialize<JsonElement>(result);
        doc.GetProperty("guidelineId").GetString().Should().Be("ADOG-STEPS-006");
        doc.GetProperty("automationStatus").GetString().Should().Be("heuristic");
        doc.GetProperty("automationReason").GetString().Should().Be("The YAML evidence needs context.");
    }

    [Fact]
    public async Task GetGuidelineAutomationAsync_GivenInvalidId_ShouldReturnErrorResponse()
    {
        // Arrange
        GuidelineResources sut = MakeSut();

        // Act
        string result = await sut.GetGuidelineAutomationAsync("not-a-valid-id");

        // Assert
        JsonElement doc = Deserialize<JsonElement>(result);
        doc.GetProperty("error").GetString().Should().Contain("not a valid guideline ID");
    }

    [Fact]
    public async Task GetGuidelineAsync_GivenGuidelineWithFix_ShouldIncludeFixInResponse()
    {
        // Arrange
        GuidelineDefinition guideline = new(
            new GuidelineId("ADOG-STEPS-006"),
            GuidelineCategory.Steps,
            GuidelineSeverity.Do,
            "Set a timeout",
            "Always set a timeout on tasks.",
            Rationale: "Prevents runaway tasks.",
            Tags: ["tasks", "timeout"],
            DetectionHints: [],
            Fix: new FixGuidance("Add timeoutInMinutes", Before: "- task: Foo@1", After: "- task: Foo@1\n  timeoutInMinutes: 10"),
            References: ["https://example.com/docs"]);

        IGuidelineRepository repo = Substitute.For<IGuidelineRepository>();
        repo.FindById(Arg.Any<GuidelineId>()).Returns(guideline);
        GuidelineResources sut = MakeSutWithRepo(repo);

        // Act
        string result = await sut.GetGuidelineAsync("ADOG-STEPS-006");

        // Assert
        JsonElement doc = Deserialize<JsonElement>(result);
        doc.GetProperty("rationale").GetString().Should().Be("Prevents runaway tasks.");
        doc.GetProperty("tags").EnumerateArray().Should().HaveCount(2);
        doc.GetProperty("references").EnumerateArray().Should().HaveCount(1);
        JsonElement fix = doc.GetProperty("fix");
        fix.GetProperty("summary").GetString().Should().Be("Add timeoutInMinutes");
        fix.GetProperty("before").GetString().Should().Be("- task: Foo@1");
        fix.GetProperty("after").GetString().Should().Contain("timeoutInMinutes");
    }

    [Fact]
    public async Task GetGuidelineAsync_GivenGuidelineWithNullOptionalFields_ShouldOmitNullsFromJson()
    {
        // Arrange
        GuidelineDefinition guideline = MakeGuideline("ADOG-STEPS-001");
        IGuidelineRepository repo = Substitute.For<IGuidelineRepository>();
        repo.FindById(Arg.Any<GuidelineId>()).Returns(guideline);
        GuidelineResources sut = MakeSutWithRepo(repo);

        // Act
        string result = await sut.GetGuidelineAsync("ADOG-STEPS-001");

        // Assert — null optional fields must not appear in the JSON
        result.Should().NotContain("\"rationale\"");
        result.Should().NotContain("\"tags\"");
        result.Should().NotContain("\"fix\"");
        result.Should().NotContain("\"references\"");
        result.Should().NotContain("\"detectionHints\"");
    }

    [Fact]
    public async Task GetGuidelineAsync_GivenGuidelineWithDetectionHints_ShouldIncludeHintsInResponse()
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
        GuidelineResources sut = MakeSutWithRepo(repo);

        // Act
        string result = await sut.GetGuidelineAsync("ADOG-STEPS-001");

        // Assert
        JsonElement doc = Deserialize<JsonElement>(result);
        JsonElement hints = doc.GetProperty("detectionHints");
        hints.GetArrayLength().Should().Be(1);
        hints[0].GetProperty("kind").GetString().Should().Be("regex");
        hints[0].GetProperty("scope").GetString().Should().Be("step");
    }
}
