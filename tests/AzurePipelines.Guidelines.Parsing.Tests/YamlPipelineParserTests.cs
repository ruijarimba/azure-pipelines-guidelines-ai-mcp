using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Parsing;
using AzurePipelines.Guidelines.Parsing.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Parsing.Tests;

public sealed class YamlPipelineParserTests
{
    private static readonly YamlPipelineParser _parser = new();

    // ── Guard clauses ─────────────────────────────────────────────────────────

    [Fact]
    public void Parse_GivenNullYaml_ShouldThrowArgumentException()
    {
        Action act = () => _parser.Parse(null!, "file.yml");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Parse_GivenEmptyYaml_ShouldThrowArgumentException()
    {
        Action act = () => _parser.Parse(string.Empty, "file.yml");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Parse_GivenNullFilePath_ShouldThrowArgumentException()
    {
        Action act = () => _parser.Parse("trigger: none", null!);
        act.Should().Throw<ArgumentException>();
    }

    // ── Malformed YAML ────────────────────────────────────────────────────────

    [Fact]
    public void Parse_GivenInvalidYaml_ShouldThrowPipelineParsingException()
    {
        const string yaml = """
            key: [unclosed bracket
            """;

        Action act = () => _parser.Parse(yaml, "bad.yml");
        act.Should().Throw<PipelineParsingException>();
    }

    [Fact]
    public void Parse_GivenScalarRootDocument_ShouldThrowPipelineParsingException()
    {
        Action act = () => _parser.Parse("just a scalar", "scalar.yml");
        act.Should().Throw<PipelineParsingException>();
    }

    // ── Minimal / empty pipeline ──────────────────────────────────────────────

    [Fact]
    public void Parse_GivenMinimalTriggerOnlyYaml_ShouldReturnEmptyCollections()
    {
        string yaml = TestFixtures.Load("YamlPipelineParser/MinimalTriggerOnly.yml");

        PipelineDocument doc = _parser.Parse(yaml, "azure-pipelines.yml");

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
        string yaml = TestFixtures.Load("YamlPipelineParser/ParametersBlock.yml");

        PipelineDocument doc = _parser.Parse(yaml, "f.yml");

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
        string yaml = TestFixtures.Load("YamlPipelineParser/ParameterWithValues.yml");

        PipelineDocument doc = _parser.Parse(yaml, "f.yml");

        doc.Parameters[0].Values.Should().Equal("eastus", "westus");
    }

    [Fact]
    public void Parse_GivenMultipleParameters_ShouldPreserveDeclarationOrder()
    {
        string yaml = TestFixtures.Load("YamlPipelineParser/MultipleParameters.yml");

        PipelineDocument doc = _parser.Parse(yaml, "f.yml");

        doc.Parameters.Select(p => p.Name).Should().Equal("first", "second");
    }

    // ── Variables — sequence form ─────────────────────────────────────────────

    [Fact]
    public void Parse_GivenVariableSequence_ShouldMapNameAndValue()
    {
        string yaml = TestFixtures.Load("YamlPipelineParser/VariableSequence.yml");

        PipelineDocument doc = _parser.Parse(yaml, "f.yml");

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
        string yaml = TestFixtures.Load("YamlPipelineParser/ReadOnlyVariable.yml");

        PipelineDocument doc = _parser.Parse(yaml, "f.yml");

        doc.Variables[0].IsReadOnly.Should().BeTrue();
    }

    [Fact]
    public void Parse_GivenVariableGroupReference_ShouldMapGroup()
    {
        string yaml = TestFixtures.Load("YamlPipelineParser/VariableGroupReference.yml");

        PipelineDocument doc = _parser.Parse(yaml, "f.yml");

        VariableNode v = doc.Variables[0];
        v.Group.Should().Be("my-secrets");
        v.Name.Should().BeNull();
    }

    // ── Variables — mapping form ──────────────────────────────────────────────

    [Fact]
    public void Parse_GivenVariableMapping_ShouldMapKeyValuePairs()
    {
        string yaml = TestFixtures.Load("YamlPipelineParser/VariableMapping.yml");

        PipelineDocument doc = _parser.Parse(yaml, "f.yml");

        doc.Variables.Should().HaveCount(2);
        doc.Variables.Select(v => v.Name).Should().Contain("buildConfig").And.Contain("region");
    }

    // ── Steps (top-level) ─────────────────────────────────────────────────────

    [Fact]
    public void Parse_GivenTopLevelSteps_ShouldMapTaskStep()
    {
        string yaml = TestFixtures.Load("YamlPipelineParser/TopLevelSteps.yml");

        PipelineDocument doc = _parser.Parse(yaml, "f.yml");

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

        PipelineDocument doc = _parser.Parse(yaml, "f.yml");

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

        PipelineDocument doc = _parser.Parse(yaml, "f.yml");

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

        PipelineDocument doc = _parser.Parse(yaml, "f.yml");

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

        PipelineDocument doc = _parser.Parse(yaml, "f.yml");

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

        PipelineDocument doc = _parser.Parse(yaml, "f.yml");

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

        PipelineDocument doc = _parser.Parse(yaml, "f.yml");

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

        PipelineDocument doc = _parser.Parse(yaml, "f.yml");

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

        PipelineDocument doc = _parser.Parse(yaml, "f.yml");

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

        PipelineDocument doc = _parser.Parse(yaml, "f.yml");

        doc.Steps[0].Line.Should().BeGreaterThan(0);
    }

    // ── Edge cases: empty document / non-mapping items ─────────────────────

    [Fact]
    public void Parse_GivenEmptyYamlDocument_ShouldThrowPipelineParsingException()
    {
        // A YAML stream with a document that contains only "---" produces an empty document.
        Action act = () => _parser.Parse("---", "empty.yml");
        act.Should().Throw<PipelineParsingException>();
    }

    [Fact]
    public void Parse_GivenParameterItemThatIsNotAMapping_ShouldSkipIt()
    {
        const string yaml = """
            parameters:
              - scalarValue
            """;

        PipelineDocument doc = _parser.Parse(yaml, "f.yml");

        doc.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void Parse_GivenVariablesAsScalar_ShouldReturnNoVariables()
    {
        const string yaml = """
            variables: justAScalar
            """;

        PipelineDocument doc = _parser.Parse(yaml, "f.yml");

        doc.Variables.Should().BeEmpty();
    }

    [Fact]
    public void Parse_GivenVariableSequenceWithNonMappingItem_ShouldSkipIt()
    {
        const string yaml = """
            variables:
              - scalarValue
              - name: buildConfig
                value: Release
            """;

        PipelineDocument doc = _parser.Parse(yaml, "f.yml");

        doc.Variables.Should().ContainSingle(v => v.Name == "buildConfig");
    }

    [Fact]
    public void Parse_GivenStageWithNonMappingItem_ShouldSkipIt()
    {
        const string yaml = """
            stages:
              - scalarStage
              - stage: CI
                jobs: []
            """;

        PipelineDocument doc = _parser.Parse(yaml, "f.yml");

        doc.Stages.Should().ContainSingle(s => s.Name == "CI");
    }

    [Fact]
    public void Parse_GivenJobWithNonMappingItem_ShouldSkipIt()
    {
        const string yaml = """
            jobs:
              - scalarJob
              - job: Build
                steps: []
            """;

        PipelineDocument doc = _parser.Parse(yaml, "f.yml");

        doc.Jobs.Should().ContainSingle(j => j.Name == "Build");
    }

    [Fact]
    public void Parse_GivenStepWithNonMappingItem_ShouldSkipIt()
    {
        const string yaml = """
            steps:
              - scalarStep
              - task: SomeTask@1
            """;

        PipelineDocument doc = _parser.Parse(yaml, "f.yml");

        doc.Steps.Should().ContainSingle(s => s.Task == "SomeTask@1");
    }

    // ── BoolOrFalse casing ──────────────────────────────────────────────────

    [Theory]
    [InlineData("True")]
    [InlineData("TRUE")]
    public void Parse_GivenReadOnlyVariableWithAlternateTrueCasing_ShouldSetIsReadOnly(string trueValue)
    {
        string yaml = $"""
            variables:
              - name: buildConfig
                value: Release
                readonly: {trueValue}
            """;

        PipelineDocument doc = _parser.Parse(yaml, "f.yml");

        doc.Variables[0].IsReadOnly.Should().BeTrue();
    }

    // ── Parameters: skipped items ───────────────────────────────────────────

    [Fact]
    public void Parse_GivenParameterItemWithoutName_ShouldSkipIt()
    {
        const string yaml = """
            parameters:
              - type: string
                default: dev
            """;

        PipelineDocument doc = _parser.Parse(yaml, "f.yml");

        doc.Parameters.Should().BeEmpty();
    }

    // ── Variables: mapping form with non-scalar value ───────────────────────

    [Fact]
    public void Parse_GivenVariableMappingWithNonScalarValue_ShouldSkipNullValue()
    {
        const string yaml = """
            variables:
              buildConfig: Release
              nested:
                - item1
            """;

        PipelineDocument doc = _parser.Parse(yaml, "f.yml");

        doc.Variables.Should().ContainSingle(v => v.Name == "buildConfig");
    }
}
