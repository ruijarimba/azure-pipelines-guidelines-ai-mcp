using AzurePipelines.Guidelines.Core;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Core.Tests;

public sealed class DiagnosticTests
{
    // ── ToString ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ToString_GivenDiagnosticWithKnownLine_ShouldIncludeLineNumber()
    {
        // Arrange
        Diagnostic diagnostic = new(
            new GuidelineId("ADOG-JOBS-006"),
            DiagnosticSeverity.Error,
            "Job 'build' is missing 'timeoutInMinutes'.",
            "azure-pipelines.yml",
            Line: 10,
            Column: null);

        // Act
        string result = diagnostic.ToString();

        // Assert
        result.Should().Be("[Error] ADOG-JOBS-006: Job 'build' is missing 'timeoutInMinutes'. (azure-pipelines.yml:10)");
    }

    [Fact]
    public void ToString_GivenDiagnosticWithNullLine_ShouldShowQuestionMark()
    {
        // Arrange
        Diagnostic diagnostic = new(
            new GuidelineId("ADOG-JOBS-001"),
            DiagnosticSeverity.Info,
            "Job '(unnamed)' has no checkout step.",
            "azure-pipelines.yml",
            Line: null,
            Column: null);

        // Act
        string result = diagnostic.ToString();

        // Assert
        result.Should().Be("[Info] ADOG-JOBS-001: Job '(unnamed)' has no checkout step. (azure-pipelines.yml:?)");
    }

    [Fact]
    public void ToString_GivenWarningSeverity_ShouldShowWarningSeverityLabel()
    {
        // Arrange
        Diagnostic diagnostic = new(
            new GuidelineId("ADOG-STEPS-001"),
            DiagnosticSeverity.Warning,
            "Step uses macro syntax.",
            "pipeline.yml",
            Line: 42,
            Column: null);

        // Act
        string result = diagnostic.ToString();

        // Assert
        result.Should().Be("[Warning] ADOG-STEPS-001: Step uses macro syntax. (pipeline.yml:42)");
    }
}
