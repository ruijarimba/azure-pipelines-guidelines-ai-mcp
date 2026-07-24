using AzurePipelines.Guidelines.Core;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests;

public sealed class GuidelineAutomationMetadataProviderTests
{
    [Theory]
    [InlineData("ADOG-STEPS-001", GuidelineAutomationStatus.Enforceable)]
    [InlineData("ADOG-JOBS-001", GuidelineAutomationStatus.Enforceable)]
    [InlineData("ADOG-JOBS-003", GuidelineAutomationStatus.Enforceable)]
    [InlineData("ADOG-GENERAL-001", GuidelineAutomationStatus.Enforceable)]
    [InlineData("ADOG-STEPS-007", GuidelineAutomationStatus.Enforceable)]
    [InlineData("ADOG-STEPS-010", GuidelineAutomationStatus.Enforceable)]
    [InlineData("ADOG-VARIABLES-005", GuidelineAutomationStatus.Enforceable)]
    [InlineData("ADOG-STEPS-006", GuidelineAutomationStatus.Heuristic)]
    [InlineData("ADOG-VARIABLES-006", GuidelineAutomationStatus.Heuristic)]
    [InlineData("ADOG-STEPS-008", GuidelineAutomationStatus.NotAutomatable)]
    public void GetAutomationMetadata_GivenKnownGuideline_ShouldReturnExpectedStatus(
        string guidelineId,
        GuidelineAutomationStatus expectedStatus)
    {
        // Arrange
        GuidelineAutomationMetadataProvider provider = new();

        // Act
        GuidelineAutomationMetadata? metadata = provider.GetAutomationMetadata(new GuidelineId(guidelineId));

        // Assert
        metadata.Should().NotBeNull();
        metadata!.Status.Should().Be(expectedStatus);
        metadata.Reason.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GetAutomationMetadata_GivenUnknownGuideline_ShouldReturnNull()
    {
        // Arrange
        GuidelineAutomationMetadataProvider provider = new();

        // Act
        GuidelineAutomationMetadata? metadata = provider.GetAutomationMetadata(new GuidelineId("ADOG-STEPS-999"));

        // Assert
        metadata.Should().BeNull();
    }
}
