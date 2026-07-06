using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Parsing;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Parsing.Tests;

public sealed class YamlPipelineParserTests
{
    private static readonly YamlPipelineParser Parser = new();

    // ── Guard clauses ─────────────────────────────────────────────────────────

    [Fact]
    public void Parse_GivenNullYaml_ShouldThrowArgumentException()
    {
        Action act = () => Parser.Parse(null!, "file.yml");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Parse_GivenEmptyYaml_ShouldThrowArgumentException()
    {
        Action act = () => Parser.Parse(string.Empty, "file.yml");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Parse_GivenNullFilePath_ShouldThrowArgumentException()
    {
        Action act = () => Parser.Parse("trigger: none", null!);
        act.Should().Throw<ArgumentException>();
    }

    // ── Malformed YAML ────────────────────────────────────────────────────────

    [Fact]
    public void Parse_GivenInvalidYaml_ShouldThrowPipelineParsingException()
    {
        const string yaml = """
            key: [unclosed bracket
            """;

        Action act = () => Parser.Parse(yaml, "bad.yml");
        act.Should().Throw<PipelineParsingException>();
    }

    [Fact]
    public void Parse_GivenScalarRootDocument_ShouldThrowPipelineParsingException()
    {
        Action act = () => Parser.Parse("just a scalar", "scalar.yml");
        act.Should().Throw<PipelineParsingException>();
    }

    // ── Minimal / empty pipeline ──────────────────────────────────────────────

    [Fact]
    public void Parse_GivenMinimalTriggerOnlyYaml_ShouldReturnEmptyCollections()
    {
        const string yaml = """
            trigger:
              - main
            """;

        PipelineDocument doc = Parser.Parse(yaml, "azure-pipelines.yml");

        doc.FilePath.Should().Be("azure-pipelines.yml");
        doc.RawContent.Should().Be(yaml);
        doc.Parameters.Should().BeEmpty();
        doc.Variables.Should().BeEmpty();
        doc.Stages.Should().BeEmpty();
        doc.Jobs.Should().BeEmpty();
        doc.Steps.Should().BeEmpty();
    }

    // ── Parameters ────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_GivenParametersBlock_ShouldMapNameAndType()
    {
        const string yaml = """
            parameters:
              - name: environment
                type: string
                default: dev
            """;

        PipelineDocument doc = Parser.Parse(yaml, "f.yml");

        doc.Parameters.Should().HaveCount(1);
        ParameterNode p = doc.Parameters[0];
        p.Name.Should().Be("environment");
        p.Type.Should().Be("string");
        p.Default.Should().Be("dev");
        p.Values.Should().BeEmpty();
    }

    [Fact]
    public void Parse_GivenParameterWithValues_ShouldMapValuesList()
    {
        const string yaml = """
            parameters:
              - name: region
                type: string
                values:
                  - eastus
                  - westus
            """;

        PipelineDocument doc = Parser.Parse(yaml, "f.yml");

        doc.Parameters[0].Values.Should().Equal("eastus", "westus");
    }

    [Fact]
    public void Parse_GivenMultipleParameters_ShouldPreserveDeclarationOrder()
    {
        const string yaml = """
            parameters:
              - name: first
                type: string
              - name: second
                type: boolean
            """;

        PipelineDocument doc = Parser.Parse(yaml, "f.yml");

        doc.Parameters.Select(p => p.Name).Should().Equal("first", "second");
    }

    // ── Variables — sequence form ─────────────────────────────────────────────

    [Fact]
    public void Parse_GivenVariableSequence_ShouldMapNameAndValue()
    {
        const string yaml = """
            variables:
              - name: buildConfig
                value: Release
            """;

        PipelineDocument doc = Parser.Parse(yaml, "f.yml");

        doc.Variables.Should().HaveCount(1);
        VariableNode v = doc.Variables[0];
        v.Name.Should().Be("buildConfig");
        v.Value.Should().Be("Release");
        v.IsReadOnly.Should().BeFalse();
        v.Group.Should().BeNull();
    }

    [Fact]
    public void Parse_GivenReadOnlyVariable_ShouldSetIsReadOnly()
    {
        const string yaml = """
            variables:
              - name: version
                value: "1.0"
                readonly: true
            """;

        PipelineDocument doc = Parser.Parse(yaml, "f.yml");

        doc.Variables[0].IsReadOnly.Should().BeTrue();
    }

    [Fact]
    public void Parse_GivenVariableGroupReference_ShouldMapGroup()
    {
        const string yaml = """
            variables:
              - group: my-secrets
            """;

        PipelineDocument doc = Parser.Parse(yaml, "f.yml");

        VariableNode v = doc.Variables[0];
        v.Group.Should().Be("my-secrets");
        v.Name.Should().BeNull();
    }

    // ── Variables — mapping form ──────────────────────────────────────────────

    [Fact]
    public void Parse_GivenVariableMapping_ShouldMapKeyValuePairs()
    {
        const string yaml = """
            variables:
              buildConfig: Release
              region: eastus
            """;

        PipelineDocument doc = Parser.Parse(yaml, "f.yml");

        doc.Variables.Should().HaveCount(2);
        doc.Variables.Select(v => v.Name).Should().Contain("buildConfig").And.Contain("region");
    }

    // ── Steps (top-level) ─────────────────────────────────────────────────────

    [Fact]
    public void Parse_GivenTopLevelSteps_ShouldMapTaskStep()
    {
        const string yaml = """
            steps:
              - task: AzureCLI@2
                displayName: Deploy
                timeoutInMinutes: 10
            """;

        PipelineDocument doc = Parser.Parse(yaml, "f.yml");

        doc.Steps.Should().HaveCount(1);
        StepNode s = doc.Steps[0];
        s.Task.Should().Be("AzureCLI@2");
        s.DisplayName.Should().Be("Deploy");
        s.TimeoutInMinutes.Should().Be(10);
        s.IsCheckout.Should().BeFalse();
    }

    [Fact]
    public void Parse_GivenCheckoutStep_ShouldSetIsCheckout()
    {
        const string yaml = """
            steps:
              - checkout: self
            """;

        PipelineDocument doc = Parser.Parse(yaml, "f.yml");

        doc.Steps[0].IsCheckout.Should().BeTrue();
        doc.Steps[0].Task.Should().BeNull();
    }

    [Fact]
    public void Parse_GivenScriptStep_ShouldMapScript()
    {
        const string yaml = """
            steps:
              - script: echo hello
                displayName: Greet
            """;

        PipelineDocument doc = Parser.Parse(yaml, "f.yml");

        doc.Steps[0].Script.Should().Be("echo hello");
        doc.Steps[0].Task.Should().BeNull();
    }

    [Fact]
    public void Parse_GivenStepWithNoTimeout_ShouldHaveNullTimeout()
    {
        const string yaml = """
            steps:
              - task: SomeTask@1
            """;

        PipelineDocument doc = Parser.Parse(yaml, "f.yml");

        doc.Steps[0].TimeoutInMinutes.Should().BeNull();
    }

    // ── Jobs (top-level) ──────────────────────────────────────────────────────

    [Fact]
    public void Parse_GivenTopLevelJob_ShouldMapJobFields()
    {
        const string yaml = """
            jobs:
              - job: Build
                displayName: Build step
                timeoutInMinutes: 60
                steps:
                  - script: dotnet build
            """;

        PipelineDocument doc = Parser.Parse(yaml, "f.yml");

        doc.Jobs.Should().HaveCount(1);
        JobNode j = doc.Jobs[0];
        j.Name.Should().Be("Build");
        j.DisplayName.Should().Be("Build step");
        j.TimeoutInMinutes.Should().Be(60);
        j.Steps.Should().HaveCount(1);
    }

    [Fact]
    public void Parse_GivenJobWithNoTimeout_ShouldHaveNullTimeout()
    {
        const string yaml = """
            jobs:
              - job: Build
                steps: []
            """;

        PipelineDocument doc = Parser.Parse(yaml, "f.yml");

        doc.Jobs[0].TimeoutInMinutes.Should().BeNull();
    }

    // ── Stages ────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_GivenStage_ShouldMapStageName()
    {
        const string yaml = """
            stages:
              - stage: CI
                displayName: Continuous Integration
                jobs:
                  - job: Build
                    steps: []
            """;

        PipelineDocument doc = Parser.Parse(yaml, "f.yml");

        doc.Stages.Should().HaveCount(1);
        StageNode s = doc.Stages[0];
        s.Name.Should().Be("CI");
        s.DisplayName.Should().Be("Continuous Integration");
        s.Jobs.Should().HaveCount(1);
    }

    // ── AllJobs / AllSteps helpers ────────────────────────────────────────────

    [Fact]
    public void AllJobs_ShouldIncludeJobsInsideStages()
    {
        const string yaml = """
            stages:
              - stage: CI
                jobs:
                  - job: Build
                    steps: []
              - stage: CD
                jobs:
                  - job: Deploy
                    steps: []
            jobs:
              - job: Standalone
                steps: []
            """;

        PipelineDocument doc = Parser.Parse(yaml, "f.yml");

        doc.AllJobs.Select(j => j.Name)
            .Should().Contain("Build")
            .And.Contain("Deploy")
            .And.Contain("Standalone");
    }

    [Fact]
    public void AllSteps_ShouldIncludeStepsInsideJobsInsideStages()
    {
        const string yaml = """
            stages:
              - stage: CI
                jobs:
                  - job: Build
                    steps:
                      - task: DotNetCoreCLI@2
            """;

        PipelineDocument doc = Parser.Parse(yaml, "f.yml");

        doc.AllSteps.Should().HaveCount(1);
        doc.AllSteps.First().Task.Should().Be("DotNetCoreCLI@2");
    }

    // ── Line numbers ──────────────────────────────────────────────────────────

    [Fact]
    public void Parse_GivenStepWithKnownPosition_ShouldReportLineNumber()
    {
        const string yaml = """
            steps:
              - task: SomeTask@1
            """;

        PipelineDocument doc = Parser.Parse(yaml, "f.yml");

        doc.Steps[0].Line.Should().BeGreaterThan(0);
    }
}
