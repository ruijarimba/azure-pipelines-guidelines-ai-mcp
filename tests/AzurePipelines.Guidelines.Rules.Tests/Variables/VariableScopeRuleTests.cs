using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Parsing;
using AzurePipelines.Guidelines.Rules.Tests.Fixtures;
using AzurePipelines.Guidelines.Rules.Variables;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests.Variables;

/// <summary>Tests detection of variables with overly broad scope.</summary>
public sealed class VariableScopeRuleTests
{
    private static readonly YamlPipelineParser _parser = new();
    private static readonly VariableScopeRule _rule = new();

    private static async Task<IReadOnlyList<Diagnostic>> EvaluateAsync(string yaml, string filePath = "pipeline.yml")
    {
        PipelineDocument doc = _parser.Parse(yaml, filePath);
        return await _rule.EvaluateAsync(doc).ToListAsync();
    }

    [Fact]
    public void GuidelineId_ShouldBeADOG_VARIABLES_005()
    {
        _rule.GuidelineId.Value.Should().Be("ADOG-VARIABLES-005");
    }

    [Fact]
    public async Task EvaluateAsync_GivenPipelineScopeVariable_ShouldReturnOneDiagnostic()
    {
        string yaml = TestFixtures.Load("Variables/VariableScope/PipelineScopeVariable.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().ContainSingle();
        diagnostics[0].GuidelineId.Value.Should().Be("ADOG-VARIABLES-005");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task EvaluateAsync_GivenJobScopeVariable_ShouldReturnNoDiagnostics()
    {
        string yaml = TestFixtures.Load("Variables/VariableScope/JobScopeVariable.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_GivenStageScopeVariable_ShouldReturnOneDiagnostic()
    {
        const string yaml = """
        stages:
        - stage: Build
          variables:
          - name: BuildConfiguration
            value: Debug
          jobs:
          - job: Build
            steps:
            - script: echo hello
        """;

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().ContainSingle();
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task EvaluateAsync_GivenVariableTemplateDocument_ShouldReturnNoDiagnostics()
    {
        const string yaml = """
        variables:
        - name: BuildConfiguration
          value: Debug
        """;

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml, "templates/variables.yml");

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_GivenPipelineVariableGroup_ShouldReturnOneDiagnostic()
    {
        const string yaml = """
        variables:
        - group: shared-settings
        jobs:
        - job: Build
          steps:
          - script: echo hello
        """;

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().ContainSingle();
        diagnostics[0].Message.Should().Contain("(unnamed)");
    }
}
