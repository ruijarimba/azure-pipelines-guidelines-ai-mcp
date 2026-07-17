using AzurePipelines.Guidelines.Core;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Core.Tests;

public sealed class GuidelineRepositoryTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static GuidelineDefinition MakeGuideline(
        string id,
        GuidelineCategory category = GuidelineCategory.General,
        GuidelineSeverity severity = GuidelineSeverity.Do) =>
        new(
            new GuidelineId(id),
            category,
            severity,
            Title: $"Title for {id}",
            Description: $"Description for {id}",
            Rationale: null,
            Tags: [],
            DetectionHints: [],
            Fix: null,
            References: []);

    // ── Constructor ───────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_GivenNullList_ShouldThrowArgumentNullException()
    {
        // Arrange / Act
        Action act = () => _ = new GuidelineRepository(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public void GetAll_GivenEmptyList_ShouldReturnEmptyCollection()
    {
        // Arrange
        GuidelineRepository sut = new([]);

        // Act
        IReadOnlyList<GuidelineDefinition> result = sut.GetAll();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetAll_GivenTwoGuidelines_ShouldReturnBoth()
    {
        // Arrange
        GuidelineDefinition[] guidelines =
        [
            MakeGuideline("ADOG-STEPS-001"),
            MakeGuideline("ADOG-STEPS-002"),
        ];
        GuidelineRepository sut = new(guidelines);

        // Act
        IReadOnlyList<GuidelineDefinition> result = sut.GetAll();

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(guidelines);
    }

    // ── FindById ──────────────────────────────────────────────────────────────

    [Fact]
    public void FindById_GivenNullId_ShouldThrowArgumentNullException()
    {
        // Arrange
        GuidelineRepository sut = new([]);

        // Act
        Action act = () => _ = sut.FindById(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FindById_GivenKnownId_ShouldReturnGuideline()
    {
        // Arrange
        GuidelineDefinition target = MakeGuideline("ADOG-STEPS-001");
        GuidelineRepository sut = new([target, MakeGuideline("ADOG-STEPS-002")]);

        // Act
        GuidelineDefinition? result = sut.FindById(new GuidelineId("ADOG-STEPS-001"));

        // Assert
        result.Should().NotBeNull();
        result!.Id.Value.Should().Be("ADOG-STEPS-001");
    }

    [Fact]
    public void FindById_GivenUnknownId_ShouldReturnNull()
    {
        // Arrange
        GuidelineRepository sut = new([MakeGuideline("ADOG-STEPS-001")]);

        // Act
        GuidelineDefinition? result = sut.FindById(new GuidelineId("ADOG-STEPS-099"));

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FindById_GivenEmptyRepository_ShouldReturnNull()
    {
        // Arrange
        GuidelineRepository sut = new([]);

        // Act
        GuidelineDefinition? result = sut.FindById(new GuidelineId("ADOG-STEPS-001"));

        // Assert
        result.Should().BeNull();
    }

    // ── GetByCategory ─────────────────────────────────────────────────────────

    [Fact]
    public void GetByCategory_GivenMatchingGuidelines_ShouldReturnOnlyThoseGuidelines()
    {
        // Arrange
        GuidelineDefinition stepsGuideline1 = MakeGuideline("ADOG-STEPS-001", GuidelineCategory.Steps);
        GuidelineDefinition stepsGuideline2 = MakeGuideline("ADOG-STEPS-002", GuidelineCategory.Steps);
        GuidelineDefinition jobsGuideline = MakeGuideline("ADOG-JOBS-001", GuidelineCategory.Jobs);
        GuidelineRepository sut = new([stepsGuideline1, jobsGuideline, stepsGuideline2]);

        // Act
        IReadOnlyList<GuidelineDefinition> result = sut.GetByCategory(GuidelineCategory.Steps);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(g => g.Category.Should().Be(GuidelineCategory.Steps));
    }

    [Fact]
    public void GetByCategory_GivenNoMatchingGuidelines_ShouldReturnEmptyCollection()
    {
        // Arrange
        GuidelineRepository sut = new([MakeGuideline("ADOG-STEPS-001", GuidelineCategory.Steps)]);

        // Act
        IReadOnlyList<GuidelineDefinition> result = sut.GetByCategory(GuidelineCategory.Jobs);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetByCategory_GivenEmptyRepository_ShouldReturnEmptyCollection()
    {
        // Arrange
        GuidelineRepository sut = new([]);

        // Act
        IReadOnlyList<GuidelineDefinition> result = sut.GetByCategory(GuidelineCategory.Steps);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void RuleMetadataAttribute_GivenValidValues_ShouldExposeThem()
    {
        RuleMetadataAttribute attribute = new("ADOG-STEPS-001", "https://example.test/rule");

        attribute.RuleId.Should().Be("ADOG-STEPS-001");
        attribute.GuidelineUrl.Should().Be("https://example.test/rule");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void RuleMetadataAttribute_GivenMissingRuleId_ShouldThrow(string? ruleId)
    {
        Action act = () => _ = new RuleMetadataAttribute(ruleId!, "https://example.test");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RuleMetadataAttribute_GivenNullGuidelineUrl_ShouldThrow()
    {
        Action act = () => _ = new RuleMetadataAttribute("ADOG-STEPS-001", null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RuleMetadataAttribute_GivenEmptyGuidelineUrl_ShouldThrow()
    {
        Action act = () => _ = new RuleMetadataAttribute("ADOG-STEPS-001", "");

        act.Should().Throw<ArgumentException>();
    }
}
