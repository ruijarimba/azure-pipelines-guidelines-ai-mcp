using AzurePipelines.Guidelines.Core;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Core.Tests;

public sealed class AnalysisOptionsTests
{
    [Fact]
    public void Default_ShouldHaveMinimumSeverityInfo()
    {
        // Arrange / Act
        AnalysisOptions options = AnalysisOptions.Default;

        // Assert
        options.MinimumSeverity.Should().Be(DiagnosticSeverity.Info);
    }

    [Fact]
    public void Default_ShouldHaveEnforceableOnlyTrue()
    {
        // Arrange / Act
        AnalysisOptions options = AnalysisOptions.Default;

        // Assert
        options.EnforceableOnly.Should().BeTrue();
    }

    [Fact]
    public void Default_ShouldHaveNullIncludedCategories()
    {
        // Arrange / Act
        AnalysisOptions options = AnalysisOptions.Default;

        // Assert
        options.IncludedCategories.Should().BeNull();
    }

    [Fact]
    public void Default_ShouldHaveNullIncludedGuidelineIds()
    {
        // Arrange / Act
        AnalysisOptions options = AnalysisOptions.Default;

        // Assert
        options.IncludedGuidelineIds.Should().BeNull();
    }

    [Fact]
    public void Default_ShouldReturnSameInstance()
    {
        // Arrange / Act
        AnalysisOptions first = AnalysisOptions.Default;
        AnalysisOptions second = AnalysisOptions.Default;

        // Assert
        first.Should().BeSameAs(second);
    }

    [Fact]
    public void Constructor_GivenCustomMinimumSeverity_ShouldSetMinimumSeverity()
    {
        // Arrange / Act
        AnalysisOptions options = new(MinimumSeverity: DiagnosticSeverity.Error);

        // Assert
        options.MinimumSeverity.Should().Be(DiagnosticSeverity.Error);
    }

    [Fact]
    public void Constructor_GivenIncludedCategories_ShouldSetIncludedCategories()
    {
        // Arrange
        GuidelineCategory[] categories = [GuidelineCategory.Steps, GuidelineCategory.Variables];

        // Act
        AnalysisOptions options = new(IncludedCategories: categories);

        // Assert
        options.IncludedCategories.Should().BeEquivalentTo(categories);
    }
}
