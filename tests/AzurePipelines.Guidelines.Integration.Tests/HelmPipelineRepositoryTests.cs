using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Integration.Tests;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1515:Consider making public types internal",
    Justification = "xUnit test discovery requires a public test class.")]
public sealed class HelmPipelineRepositoryTests : PipelineRepositoryIntegrationTestsBase
{
    protected override string RepositoryFolder => "helm-pipeline-templates";

    protected override int ExpectedYamlFileCount => 4;

    protected override IReadOnlyCollection<GuidelineId> ExpectedGuidelineIds
    {
        get;
    } =
    [
        new("ADOG-GENERAL-001"),
        new("ADOG-GENERAL-004"),
        new("ADOG-GENERAL-005"),
        new("ADOG-JOBS-001"),
        new("ADOG-JOBS-005"),
        new("ADOG-JOBS-006"),
        new("ADOG-JOBS-007"),
        new("ADOG-PARAMETERS-002"),
        new("ADOG-PIPELINES-001"),
        new("ADOG-STEPS-003"),
        new("ADOG-STEPS-004"),
        new("ADOG-STEPS-007"),
        new("ADOG-STEPS-009"),
        new("ADOG-VARIABLES-001"),
        new("ADOG-VARIABLES-002"),
        new("ADOG-VARIABLES-004"),
        new("ADOG-VARIABLES-005"),
    ];
}
