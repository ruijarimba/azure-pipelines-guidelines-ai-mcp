using AzurePipelines.Guidelines.Core;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Core.Tests;

public sealed class GuidelineSeverityExtensionsTests
{
    [Theory]
    [InlineData(GuidelineSeverity.Do, DiagnosticSeverity.Error)]
    [InlineData(GuidelineSeverity.DoNot, DiagnosticSeverity.Error)]
    [InlineData(GuidelineSeverity.Avoid, DiagnosticSeverity.Warning)]
    [InlineData(GuidelineSeverity.Consider, DiagnosticSeverity.Info)]
    public void ToDiagnosticSeverity_GivenKnownSeverity_ShouldReturnMappedValue(
        GuidelineSeverity input,
        DiagnosticSeverity expected)
    {
        // Arrange / Act
        DiagnosticSeverity result = input.ToDiagnosticSeverity();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ToDiagnosticSeverity_GivenUndefinedSeverity_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        GuidelineSeverity undefined = (GuidelineSeverity)999;

        // Act
        Action act = () => undefined.ToDiagnosticSeverity();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
