using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Integration.Tests;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1515:Consider making public types internal",
    Justification = "xUnit test discovery requires a public test class.")]
public sealed class TerraformPipelineRepositoryTests : PipelineRepositoryIntegrationTestsBase
{
    protected override string RepositoryFolder => "terraform-pipeline-templates";

    protected override int ExpectedYamlFileCount => 3;

    protected override IReadOnlyCollection<GuidelineId> ExpectedGuidelineIds
    {
        get;
    } =
    [
        new("ADOG-GENERAL-004"),
        new("ADOG-GENERAL-005"),
        new("ADOG-GENERAL-006"),
        new("ADOG-GENERAL-007"),
        new("ADOG-JOBS-001"),
        new("ADOG-JOBS-004"),
        new("ADOG-JOBS-006"),
        new("ADOG-JOBS-008"),
        new("ADOG-PARAMETERS-001"),
        new("ADOG-PARAMETERS-002"),
        new("ADOG-PIPELINES-001"),
        new("ADOG-STAGES-001"),
        new("ADOG-STAGES-002"),
        new("ADOG-STEPS-005"),
        new("ADOG-STEPS-011"),
        new("ADOG-VARIABLES-003"),
        new("ADOG-VARIABLES-006"),
    ];
}
