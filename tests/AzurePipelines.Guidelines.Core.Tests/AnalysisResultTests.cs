using AzurePipelines.Guidelines.Core;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Core.Tests;

public sealed class AnalysisResultTests
{
    private static PipelineDocument MakeDocument() =>
        new("azure-pipelines.yml", "trigger: none",
            Parameters: [], Variables: [], Stages: [], Jobs: [], Steps: []);

    private static Diagnostic MakeDiagnostic() =>
        new(
            new GuidelineId("ADOG-STEPS-001"),
            DiagnosticSeverity.Warning,
            "Test finding",
            "azure-pipelines.yml",
            Line: 1,
            Column: 1);

    [Fact]
    public void IsClean_GivenNoDiagnostics_ShouldReturnTrue()
    {
        // Arrange
        AnalysisResult result = new(MakeDocument(), []);

        // Act / Assert
        result.IsClean.Should().BeTrue();
    }

    [Fact]
    public void IsClean_GivenOneDiagnostic_ShouldReturnFalse()
    {
        // Arrange
        AnalysisResult result = new(MakeDocument(), [MakeDiagnostic()]);

        // Act / Assert
        result.IsClean.Should().BeFalse();
    }

    [Fact]
    public void Document_ShouldBeTheDocumentPassedToConstructor()
    {
        // Arrange
        PipelineDocument document = MakeDocument();

        // Act
        AnalysisResult result = new(document, []);

        // Assert
        result.Document.Should().BeSameAs(document);
    }

    [Fact]
    public void Diagnostics_ShouldContainAllPassedDiagnostics()
    {
        // Arrange
        Diagnostic[] diagnostics = [MakeDiagnostic(), MakeDiagnostic()];

        // Act
        AnalysisResult result = new(MakeDocument(), diagnostics);

        // Assert
        result.Diagnostics.Should().HaveCount(2);
    }

    [Fact]
    public void SkippedRuleDetails_GivenNoSkippedGuidelines_ShouldReturnEmptyCollection()
    {
        // Arrange
        AnalysisResult result = new(MakeDocument(), []);

        // Act
        IReadOnlyList<SkippedGuideline> skippedGuidelines = result.SkippedRuleDetails;

        // Assert
        skippedGuidelines.Should().BeEmpty();
    }

    [Fact]
    public void SkippedRuleDetails_GivenSkippedGuidelines_ShouldReturnPassedDetails()
    {
        // Arrange
        SkippedGuideline expected = new(
            new GuidelineId("ADOG-STEPS-008"),
            GuidelineAutomationStatus.NotAutomatable,
            "Task selection needs external context.");
        AnalysisResult result = new(MakeDocument(), [], SkippedGuidelines: [expected]);

        // Act
        IReadOnlyList<SkippedGuideline> skippedGuidelines = result.SkippedRuleDetails;

        // Assert
        skippedGuidelines.Should().ContainSingle().Which.Should().Be(expected);
    }
}
