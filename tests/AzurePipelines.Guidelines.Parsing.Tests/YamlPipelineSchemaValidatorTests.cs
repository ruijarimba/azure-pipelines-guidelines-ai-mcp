using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Parsing;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Parsing.Tests;

public sealed class YamlPipelineSchemaValidatorTests
{
    private static readonly YamlPipelineSchemaValidator _validator = new();

    [Fact]
    public void Validate_GivenValidPipeline_ShouldReturnNoDiagnostics()
    {
        const string yaml = """
            trigger: none
            stages:
              - stage: Build
                jobs:
                  - job: Compile
                    steps:
                      - script: dotnet build
            """;

        _validator.Validate(yaml, "pipeline.yml").Should().BeEmpty();
    }

    [Fact]
    public void Validate_GivenStageTemplate_ShouldValidateStagesSequence()
    {
        const string yaml = """
            - stage: Build
              jobs: []
            """;

        _validator.Validate(yaml, "stages.yml", PipelineSchemaContext.Stages)
            .Should().BeEmpty();
    }

    [Fact]
    public void Validate_GivenJobTemplate_ShouldValidateSingleJobMapping()
    {
        const string yaml = """
            job: Build
            steps:
              - template: steps.yml
            """;

        _validator.Validate(yaml, "job.yml", PipelineSchemaContext.Job)
            .Should().BeEmpty();
    }

    [Fact]
    public void Validate_GivenStepTemplate_ShouldValidateSingleStepMapping()
    {
        const string yaml = """
            template: build-step.yml
            parameters:
              configuration: Release
            """;

        _validator.Validate(yaml, "step.yml", PipelineSchemaContext.Step)
            .Should().BeEmpty();
    }

    [Fact]
    public void Validate_GivenMissingStagesTemplateRoot_ShouldReportDiagnostic()
    {
        IReadOnlyList<SchemaDiagnostic> diagnostics = _validator.Validate(
            "jobs: []", "stages.yml", PipelineSchemaContext.Stages);

        diagnostics.Should().ContainSingle(d => d.Code == "ADOG-SCHEMA-006");
    }

    [Fact]
    public void Validate_GivenJobWithoutIdentifier_ShouldReportDiagnostic()
    {
        const string yaml = """
            jobs:
              - steps: []
            """;

        _validator.Validate(yaml, "pipeline.yml")
            .Should().ContainSingle(d => d.Code == "ADOG-SCHEMA-010");
    }

    [Fact]
    public void Validate_GivenUnnamedJobFixture_ShouldReportInvalidJobShape()
    {
        const string yaml = """
            jobs:
              - steps:
                  - script: echo building
            """;

        _validator.Validate(yaml, "UnnamedJob.yml")
            .Should().ContainSingle(d => d.Code == "ADOG-SCHEMA-010");
    }

    [Fact]
    public void Validate_GivenStepsWithMultipleActions_ShouldReportDiagnostic()
    {
        const string yaml = """
            steps:
              - script: echo hello
                task: Bash@3
            """;

        _validator.Validate(yaml, "steps.yml")
            .Should().ContainSingle(d => d.Code == "ADOG-SCHEMA-013");
    }

    [Fact]
    public void Validate_GivenScalarRoot_ShouldReportDiagnostic()
    {
        _validator.Validate("just a scalar", "invalid.yml")
            .Should().ContainSingle(d => d.Code == "ADOG-SCHEMA-003");
    }

    [Fact]
    public void Validate_GivenExpressionValues_ShouldRemainPermissive()
    {
        const string yaml = """
            jobs:
              - job: ${{ parameters.jobName }}
                steps:
                  - script: echo ${{ parameters.message }}
            """;

        _validator.Validate(yaml, "expression.yml").Should().BeEmpty();
    }
}
