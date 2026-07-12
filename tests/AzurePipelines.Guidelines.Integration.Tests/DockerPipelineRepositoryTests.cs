using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Integration.Tests;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1515:Consider making public types internal",
    Justification = "xUnit test discovery requires a public test class.")]
public sealed class DockerPipelineRepositoryTests : PipelineRepositoryIntegrationTestsBase
{
    protected override string RepositoryFolder => "docker-pipeline-templates";

    protected override int ExpectedYamlFileCount => 3;

    protected override IReadOnlyCollection<GuidelineId> ExpectedGuidelineIds { get; } =
    [
        new("ADOG-GENERAL-002"),
        new("ADOG-GENERAL-003"),
        new("ADOG-GENERAL-004"),
        new("ADOG-GENERAL-006"),
        new("ADOG-GENERAL-007"),
        new("ADOG-JOBS-001"),
        new("ADOG-JOBS-002"),
        new("ADOG-JOBS-003"),
        new("ADOG-JOBS-004"),
        new("ADOG-JOBS-006"),
        new("ADOG-STEPS-001"),
        new("ADOG-STEPS-002"),
        new("ADOG-STEPS-003"),
        new("ADOG-STEPS-005"),
        new("ADOG-STEPS-006"),
        new("ADOG-STEPS-008"),
        new("ADOG-STEPS-010"),
    ];
}
