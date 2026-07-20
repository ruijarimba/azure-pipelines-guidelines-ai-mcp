using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Parsing;
using AzurePipelines.Guidelines.Rules.Tests.Fixtures;
using AzurePipelines.Guidelines.Rules.Steps;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests.Steps;

/// <summary>Tests validation of parameters supplied to steps.</summary>
public sealed class StepParameterValidationRuleTests
{
    private static readonly YamlPipelineParser _parser = new();
    private static readonly StepParameterValidationRule _rule = new();

    private static async Task<IReadOnlyList<Diagnostic>> EvaluateAsync(string yaml, string filePath = "pipeline.yml")
    {
        PipelineDocument doc = _parser.Parse(yaml, filePath);
        return await _rule.EvaluateAsync(doc).ToListAsync();
    }

    [Fact]
    public void GuidelineId_ShouldBeADOG_STEPS_009()
    {
        _rule.GuidelineId.Value.Should().Be("ADOG-STEPS-009");
    }

    [Fact]
    public async Task EvaluateAsync_GivenParametersWithoutValidation_ShouldReturnOneDiagnostic()
    {
        string yaml = TestFixtures.Load("Steps/StepParameterValidation/WithoutValidation.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().ContainSingle();
        diagnostics[0].GuidelineId.Value.Should().Be("ADOG-STEPS-009");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task EvaluateAsync_GivenParametersWithValidation_ShouldReturnNoDiagnostics()
    {
        string yaml = TestFixtures.Load("Steps/StepParameterValidation/WithValidation.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().BeEmpty();
    }
}
