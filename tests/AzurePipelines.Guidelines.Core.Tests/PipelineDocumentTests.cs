using AzurePipelines.Guidelines.Core;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Core.Tests;

public sealed class PipelineDocumentTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static StepNode MakeStep(string? task = "AzureCLI@2") =>
        new(task, Script: null, DisplayName: null, TimeoutInMinutes: null,
            IsCheckout: false, Condition: null, Line: null);

    private static JobNode MakeJob(string name, params StepNode[] steps) =>
        new(name, DisplayName: null, TimeoutInMinutes: null,
            Steps: steps, Variables: [], Condition: null, Line: null);

    private static StageNode MakeStage(string name, params JobNode[] jobs) =>
        new(name, DisplayName: null, Jobs: jobs, Variables: [], Condition: null, Line: null);

    private static PipelineDocument MakeDocument(
        IReadOnlyList<StageNode>? stages = null,
        IReadOnlyList<JobNode>? jobs = null,
        IReadOnlyList<StepNode>? steps = null) =>
        new(
            FilePath: "azure-pipelines.yml",
            RawContent: string.Empty,
            Parameters: [],
            Variables: [],
            Stages: stages ?? [],
            Jobs: jobs ?? [],
            Steps: steps ?? []);

    // ── AllJobs ───────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_GivenAllProperties_ShouldExposeThem()
    {
        // Arrange
        ParameterNode parameter = new("environment", "string", "dev", []);
        VariableNode variable = new("Configuration", "Release", true, null);
        StageNode stage = MakeStage("Build");
        JobNode job = MakeJob("Test");
        StepNode step = MakeStep();

        // Act
        PipelineDocument result = new("pipeline.yml", "steps: []", [parameter], [variable],
            [stage], [job], [step]);

        // Assert
        result.FilePath.Should().Be("pipeline.yml");
        result.RawContent.Should().Be("steps: []");
        result.Parameters.Should().ContainSingle().Which.Should().Be(parameter);
        result.Variables.Should().ContainSingle().Which.Should().Be(variable);
        result.Stages.Should().ContainSingle().Which.Should().Be(stage);
        result.Jobs.Should().ContainSingle().Which.Should().Be(job);
        result.Steps.Should().ContainSingle().Which.Should().Be(step);
    }

    [Fact]
    public void AllJobs_GivenTopLevelJobsOnly_ShouldReturnThoseJobs()
    {
        // Arrange
        JobNode job1 = MakeJob("Job1");
        JobNode job2 = MakeJob("Job2");
        PipelineDocument doc = MakeDocument(jobs: [job1, job2]);

        // Act + Assert
        doc.AllJobs.Should().BeEquivalentTo(new[] { job1, job2 });
    }

    [Fact]
    public void AllJobs_GivenJobsInStages_ShouldReturnThoseJobs()
    {
        // Arrange
        JobNode job1 = MakeJob("Job1");
        JobNode job2 = MakeJob("Job2");
        StageNode stage = MakeStage("Stage1", job1, job2);
        PipelineDocument doc = MakeDocument(stages: [stage]);

        // Act + Assert
        doc.AllJobs.Should().BeEquivalentTo(new[] { job1, job2 });
    }

    [Fact]
    public void AllJobs_GivenJobsInStagesAndTopLevel_ShouldReturnAll()
    {
        // Arrange
        JobNode stageJob = MakeJob("StageJob");
        JobNode topLevelJob = MakeJob("TopJob");
        StageNode stage = MakeStage("Stage1", stageJob);
        PipelineDocument doc = MakeDocument(stages: [stage], jobs: [topLevelJob]);

        // Act + Assert
        doc.AllJobs.Should().BeEquivalentTo(new[] { stageJob, topLevelJob });
    }

    [Fact]
    public void AllJobs_GivenNoJobsOrStages_ShouldReturnEmpty()
    {
        // Arrange
        PipelineDocument doc = MakeDocument();

        // Act + Assert
        doc.AllJobs.Should().BeEmpty();
    }

    // ── AllSteps ──────────────────────────────────────────────────────────────

    [Fact]
    public void AllSteps_GivenTopLevelStepsOnly_ShouldReturnThoseSteps()
    {
        // Arrange
        StepNode step1 = MakeStep("Task1@1");
        StepNode step2 = MakeStep("Task2@1");
        PipelineDocument doc = MakeDocument(steps: [step1, step2]);

        // Act + Assert
        doc.AllSteps.Should().BeEquivalentTo(new[] { step1, step2 });
    }

    [Fact]
    public void AllSteps_GivenStepsInJobs_ShouldReturnThoseSteps()
    {
        // Arrange
        StepNode step = MakeStep("Task1@1");
        JobNode job = MakeJob("Job1", step);
        PipelineDocument doc = MakeDocument(jobs: [job]);

        // Act + Assert
        doc.AllSteps.Should().ContainSingle().Which.Should().Be(step);
    }

    [Fact]
    public void AllSteps_GivenStepsInStageJobs_ShouldReturnThoseSteps()
    {
        // Arrange
        StepNode step = MakeStep("Task1@1");
        JobNode job = MakeJob("Job1", step);
        StageNode stage = MakeStage("Stage1", job);
        PipelineDocument doc = MakeDocument(stages: [stage]);

        // Act + Assert
        doc.AllSteps.Should().ContainSingle().Which.Should().Be(step);
    }

    [Fact]
    public void AllSteps_GivenStepsAcrossAllScopes_ShouldReturnAll()
    {
        // Arrange
        StepNode stageStep = MakeStep("StageTask@1");
        StepNode jobStep = MakeStep("JobTask@1");
        StepNode topStep = MakeStep("TopTask@1");

        JobNode stageJob = MakeJob("StageJob", stageStep);
        StageNode stage = MakeStage("Stage1", stageJob);
        JobNode topJob = MakeJob("TopJob", jobStep);

        PipelineDocument doc = MakeDocument(stages: [stage], jobs: [topJob], steps: [topStep]);

        // Act + Assert
        doc.AllSteps.Should().BeEquivalentTo(new[] { stageStep, jobStep, topStep });
    }

    [Fact]
    public void AllSteps_GivenNoStepsOrJobs_ShouldReturnEmpty()
    {
        // Arrange
        PipelineDocument doc = MakeDocument();

        // Act + Assert
        doc.AllSteps.Should().BeEmpty();
    }

    // ── ToString ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ToString_GivenDocumentWithStagesJobsAndSteps_ShouldReturnConciseSummary()
    {
        // Arrange
        PipelineDocument doc = MakeDocument(
            stages: [MakeStage("S1"), MakeStage("S2")],
            jobs: [MakeJob("J1")],
            steps: [MakeStep()]);

        // Act
        string result = doc.ToString();

        // Assert
        result.Should().Be("azure-pipelines.yml (2 stages, 1 top-level jobs, 1 top-level steps)");
    }

    [Fact]
    public void ToString_GivenEmptyDocument_ShouldShowZeroCounts()
    {
        // Arrange
        PipelineDocument doc = MakeDocument();

        // Act
        string result = doc.ToString();

        // Assert
        result.Should().Be("azure-pipelines.yml (0 stages, 0 top-level jobs, 0 top-level steps)");
    }
}
