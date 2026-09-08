using AzurePipelines.Guidelines.Core;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Core.Tests;

public sealed class JobNodeTests
{
    // ── ToString ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ToString_GivenNamedJobWithLineAndSteps_ShouldReturnConciseSummary()
    {
        // Arrange
        StepNode step = new(Task: "AzureCLI@2", Script: null, DisplayName: null,
            TimeoutInMinutes: null, IsCheckout: false, Condition: null, Line: null);
        JobNode job = new("build_job", DisplayName: null, TimeoutInMinutes: null,
            Steps: [step], Variables: [], Condition: null, Line: 10);

        // Act
        string result = job.ToString();

        // Assert
        result.Should().Be("Job 'build_job' (line 10, 1 steps)");
    }

    [Fact]
    public void ToString_GivenUnnamedJobWithNoLine_ShouldUseUnnamedAndQuestionMark()
    {
        // Arrange
        JobNode job = new(Name: null, DisplayName: null, TimeoutInMinutes: null,
            Steps: [], Variables: [], Condition: null, Line: null);

        // Act
        string result = job.ToString();

        // Assert
        result.Should().Be("Job '(unnamed)' (line ?, 0 steps)");
    }

    [Fact]
    public void ToString_GivenJobWithMultipleSteps_ShouldReflectStepCount()
    {
        // Arrange
        StepNode step1 = new(Task: "Task1@1", Script: null, DisplayName: null,
            TimeoutInMinutes: null, IsCheckout: false, Condition: null, Line: null);
        StepNode step2 = new(Task: "Task2@1", Script: null, DisplayName: null,
            TimeoutInMinutes: null, IsCheckout: false, Condition: null, Line: null);
        JobNode job = new("deploy_job", DisplayName: null, TimeoutInMinutes: 60,
            Steps: [step1, step2], Variables: [], Condition: null, Line: 20);

        // Act
        string result = job.ToString();

        // Assert
        result.Should().Be("Job 'deploy_job' (line 20, 2 steps)");
    }
}
