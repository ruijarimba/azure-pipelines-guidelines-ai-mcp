using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Parsing;
using AzurePipelines.Guidelines.Rules.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests;

public sealed class EnvironmentParameterMinimizationRuleTests
{
    private static readonly YamlPipelineParser _parser = new();
    private static readonly EnvironmentParameterMinimizationRule _rule = new();

    private static async Task<IReadOnlyList<Diagnostic>> EvaluateAsync(string yaml, string filePath = "pipeline.yml")
    {
        PipelineDocument doc = _parser.Parse(yaml, filePath);
        return await _rule.EvaluateAsync(doc).ToListAsync();
    }

    [Fact]
    public void GuidelineId_ShouldBeADOG_JOBS_007()
    {
        _rule.GuidelineId.Value.Should().Be("ADOG-JOBS-007");
    }

    [Fact]
    public async Task EvaluateAsync_GivenEnvironmentParametersWithoutTemplateVariables_ShouldReturnOneDiagnostic()
    {
        string yaml = TestFixtures.Load("EnvironmentParameterMinimization/WithEnvironmentParameters.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().ContainSingle();
        diagnostics[0].GuidelineId.Value.Should().Be("ADOG-JOBS-007");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Info);
    }

    [Fact]
    public async Task EvaluateAsync_GivenVariableTemplateUsage_ShouldReturnNoDiagnostics()
    {
        string yaml = TestFixtures.Load("EnvironmentParameterMinimization/WithVariableTemplates.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().BeEmpty();
    }
}
