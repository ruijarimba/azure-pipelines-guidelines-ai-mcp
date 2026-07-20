using AzurePipelines.Guidelines.Core;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Core.Tests;

public sealed class StageNodeTests
{
    [Fact]
    public void Constructor_GivenAllProperties_ShouldExposeThem()
    {
        // Arrange
        JobNode job = new("build", "Build job", 10, [], [], "succeeded()", 2);
        VariableNode variable = new("Configuration", "Release", true, null);

        // Act
        StageNode result = new("Build", "Build stage", [job], [variable], "always()", 1);

        // Assert
        result.Name.Should().Be("Build");
        result.DisplayName.Should().Be("Build stage");
        result.Jobs.Should().ContainSingle().Which.Should().Be(job);
        result.Variables.Should().ContainSingle().Which.Should().Be(variable);
        result.Condition.Should().Be("always()");
        result.Line.Should().Be(1);
    }

    // ── ToString ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ToString_GivenNamedStageWithLineAndJobs_ShouldReturnConciseSummary()
    {
        // Arrange
        StageNode stage = new("Build", DisplayName: null, Jobs: [], Variables: [], Condition: null, Line: 5);

        // Act
        string result = stage.ToString();

        // Assert
        result.Should().Be("Stage 'Build' (line 5, 0 jobs)");
    }

    [Fact]
    public void ToString_GivenUnnamedStageWithNoLine_ShouldUseUnnamedAndQuestionMark()
    {
        // Arrange
        StageNode stage = new(Name: null, DisplayName: null, Jobs: [], Variables: [], Condition: null, Line: null);

        // Act
        string result = stage.ToString();

        // Assert
        result.Should().Be("Stage '(unnamed)' (line ?, 0 jobs)");
    }

    [Fact]
    public void ToString_GivenStageWithMultipleJobs_ShouldReflectJobCount()
    {
        // Arrange
        JobNode job1 = new("J1", DisplayName: null, TimeoutInMinutes: null, Steps: [], Variables: [], Condition: null, Line: null);
        JobNode job2 = new("J2", DisplayName: null, TimeoutInMinutes: null, Steps: [], Variables: [], Condition: null, Line: null);
        StageNode stage = new("Deploy", DisplayName: null, Jobs: [job1, job2], Variables: [], Condition: null, Line: 10);

        // Act
        string result = stage.ToString();

        // Assert
        result.Should().Be("Stage 'Deploy' (line 10, 2 jobs)");
    }
}
