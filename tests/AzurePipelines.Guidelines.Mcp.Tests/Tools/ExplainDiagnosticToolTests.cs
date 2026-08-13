using System.Text.Json;
using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Mcp.Tools;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AzurePipelines.Guidelines.Mcp.Tests.Tools;

public sealed class ExplainDiagnosticToolTests
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

    private static ExplainDiagnosticTool MakeSutWithRepo(IGuidelineRepository repo) =>
        new(repo);

    private static T Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json)!;

    // ── ExplainDiagnostic ─────────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ExplainDiagnostic_GivenNullOrWhitespaceId_ShouldReturnErrorObject(string? id)
    {
        // Arrange
        IGuidelineRepository repo = Substitute.For<IGuidelineRepository>();
        ExplainDiagnosticTool sut = MakeSutWithRepo(repo);

        // Act
        string result = sut.ExplainDiagnostic(id!);

        // Assert
        JsonElement obj = Deserialize<JsonElement>(result);
        obj.GetProperty("error").GetString().Should().Contain("required");
    }

    [Fact]
    public void ExplainDiagnostic_GivenInvalidIdFormat_ShouldReturnErrorObject()
    {
        // Arrange
        IGuidelineRepository repo = Substitute.For<IGuidelineRepository>();
        ExplainDiagnosticTool sut = MakeSutWithRepo(repo);

        // Act
        string result = sut.ExplainDiagnostic("not-a-valid-id");

        // Assert
        JsonElement obj = Deserialize<JsonElement>(result);
        obj.GetProperty("error").GetString().Should().Contain("not a valid guideline ID");
    }

    [Fact]
    public void ExplainDiagnostic_GivenUnknownId_ShouldReturnErrorObject()
    {
        // Arrange
        IGuidelineRepository repo = Substitute.For<IGuidelineRepository>();
        repo.FindById(Arg.Any<GuidelineId>()).Returns((GuidelineDefinition?)null);
        ExplainDiagnosticTool sut = MakeSutWithRepo(repo);

        // Act
        string result = sut.ExplainDiagnostic("ADOG-STEPS-001");

        // Assert
        JsonElement obj = Deserialize<JsonElement>(result);
        obj.GetProperty("error").GetString().Should().Contain("not found");
    }

    [Fact]
    public void ExplainDiagnostic_GivenKnownId_ShouldReturnGuidelineDetailWithoutDiagnosticContext()
    {
        // Arrange
        GuidelineDefinition guideline = MakeGuideline(
            "ADOG-STEPS-001", GuidelineCategory.Steps, GuidelineSeverity.Avoid, "My title", "My description");
        IGuidelineRepository repo = Substitute.For<IGuidelineRepository>();
        repo.FindById(Arg.Is<GuidelineId>(x => x.Value == "ADOG-STEPS-001")).Returns(guideline);
        ExplainDiagnosticTool sut = MakeSutWithRepo(repo);

        // Act
        string result = sut.ExplainDiagnostic("ADOG-STEPS-001");

        // Assert
        JsonElement obj = Deserialize<JsonElement>(result);
        JsonElement guidelineJson = obj.GetProperty("guideline");
        guidelineJson.GetProperty("id").GetString().Should().Be("ADOG-STEPS-001");
        guidelineJson.GetProperty("title").GetString().Should().Be("My title");
        guidelineJson.GetProperty("description").GetString().Should().Be("My description");
        guidelineJson.GetProperty("category").GetString().Should().Be("steps");
        guidelineJson.GetProperty("severity").GetString().Should().Be("avoid");
        obj.TryGetProperty("diagnostic", out _).Should().BeFalse();
    }

    [Fact]
    public void ExplainDiagnostic_GivenDiagnosticContext_ShouldEchoItBack()
    {
        // Arrange
        GuidelineDefinition guideline = MakeGuideline("ADOG-STEPS-001");
        IGuidelineRepository repo = Substitute.For<IGuidelineRepository>();
        repo.FindById(Arg.Any<GuidelineId>()).Returns(guideline);
        ExplainDiagnosticTool sut = MakeSutWithRepo(repo);

        // Act
        string result = sut.ExplainDiagnostic(
            "ADOG-STEPS-001",
            message: "Avoid inline scripts.",
            filePath: "pipelines/build.yml",
            line: 12,
            column: 3);

        // Assert
        JsonElement obj = Deserialize<JsonElement>(result);
        JsonElement diagnostic = obj.GetProperty("diagnostic");
        diagnostic.GetProperty("message").GetString().Should().Be("Avoid inline scripts.");
        diagnostic.GetProperty("filePath").GetString().Should().Be("pipelines/build.yml");
        diagnostic.GetProperty("line").GetInt32().Should().Be(12);
        diagnostic.GetProperty("column").GetInt32().Should().Be(3);
    }

    [Fact]
    public void ExplainDiagnostic_GivenMetadataProvider_ShouldIncludeAutomationStatusAndReason()
    {
        // Arrange
        GuidelineDefinition guideline = MakeGuideline("ADOG-STEPS-001");
        IGuidelineRepository repo = Substitute.For<IGuidelineRepository>();
        repo.FindById(Arg.Any<GuidelineId>()).Returns(guideline);
        IGuidelineAutomationMetadataProvider metadataProvider = Substitute.For<IGuidelineAutomationMetadataProvider>();
        metadataProvider.GetAutomationMetadata(guideline.Id).Returns(
            new GuidelineAutomationMetadata(GuidelineAutomationStatus.NotAutomatable, "The rule needs repository context."));
        ExplainDiagnosticTool sut = new(repo, metadataProvider);

        // Act
        string result = sut.ExplainDiagnostic("ADOG-STEPS-001");

        // Assert
        JsonElement obj = Deserialize<JsonElement>(result);
        JsonElement guidelineJson = obj.GetProperty("guideline");
        guidelineJson.GetProperty("automationStatus").GetString().Should().Be("notautomatable");
        guidelineJson.GetProperty("automationReason").GetString().Should().Be("The rule needs repository context.");
    }

    [Fact]
    public void ExplainDiagnostic_GivenNoAutomationProvider_ShouldReturnDefaultAutomationValues()
    {
        // Arrange
        GuidelineDefinition guideline = MakeGuideline("ADOG-STEPS-001");
        IGuidelineRepository repo = Substitute.For<IGuidelineRepository>();
        repo.FindById(Arg.Any<GuidelineId>()).Returns(guideline);
        ExplainDiagnosticTool sut = MakeSutWithRepo(repo);

        // Act
        string result = sut.ExplainDiagnostic("ADOG-STEPS-001");

        // Assert
        JsonElement obj = Deserialize<JsonElement>(result);
        JsonElement guidelineJson = obj.GetProperty("guideline");
        guidelineJson.GetProperty("automationStatus").GetString().Should().Be("notautomatable");
        guidelineJson.GetProperty("automationReason").GetString().Should().Be("No local automation metadata is available.");
    }
}
