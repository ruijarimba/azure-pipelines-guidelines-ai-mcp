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
}
