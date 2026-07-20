using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Parsing;
using AzurePipelines.Guidelines.Rules.Tests.Fixtures;
using AzurePipelines.Guidelines.Rules.Steps;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests.Steps;

/// <summary>Tests environment variable handling for task steps.</summary>
public sealed class TaskEnvironmentVariablesRuleTests
{
    private static readonly YamlPipelineParser _parser = new();
    private static readonly TaskEnvironmentVariablesRule _rule = new();

    private static async Task<IReadOnlyList<Diagnostic>> EvaluateAsync(string yaml, string filePath = "pipeline.yml")
    {
        PipelineDocument doc = _parser.Parse(yaml, filePath);
        return await _rule.EvaluateAsync(doc).ToListAsync();
    }

    [Fact]
    public void GuidelineId_ShouldBeADOG_STEPS_002()
    {
        _rule.GuidelineId.Value.Should().Be("ADOG-STEPS-002");
    }

    [Fact]
    public async Task EvaluateAsync_GivenStepUsingVariablesWithoutEnv_ShouldReturnOneDiagnostic()
    {
        string yaml = TestFixtures.Load("Steps/TaskEnvironmentVariables/UsesVariablesWithoutEnv.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().ContainSingle();
        diagnostics[0].GuidelineId.Value.Should().Be("ADOG-STEPS-002");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Info);
    }

    [Fact]
    public async Task EvaluateAsync_GivenStepWithEnvBlock_ShouldReturnNoDiagnostics()
    {
        string yaml = TestFixtures.Load("Steps/TaskEnvironmentVariables/UsesVariablesWithEnv.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().BeEmpty();
    }
}
