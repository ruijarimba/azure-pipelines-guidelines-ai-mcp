using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Parsing;
using AzurePipelines.Guidelines.Rules.Tests.Fixtures;
using AzurePipelines.Guidelines.Rules.Steps;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests.Steps;

/// <summary>Tests retry configuration for pipeline steps.</summary>
public sealed class StepRetryRuleTests
{
    private static readonly YamlPipelineParser _parser = new();
    private static readonly StepRetryRule _rule = new();

    private static async Task<IReadOnlyList<Diagnostic>> EvaluateAsync(string yaml, string filePath = "pipeline.yml")
    {
        PipelineDocument doc = _parser.Parse(yaml, filePath);
        return await _rule.EvaluateAsync(doc).ToListAsync();
    }

    [Fact]
    public void GuidelineId_ShouldBeADOG_STEPS_005()
    {
        _rule.GuidelineId.Value.Should().Be("ADOG-STEPS-005");
    }

    [Fact]
    public async Task EvaluateAsync_GivenStepWithoutRetryCount_ShouldReturnOneDiagnostic()
    {
        string yaml = TestFixtures.Load("Steps/StepRetry/WithoutRetryCount.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().ContainSingle();
        diagnostics[0].GuidelineId.Value.Should().Be("ADOG-STEPS-005");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Info);
    }

    [Fact]
    public async Task EvaluateAsync_GivenStepWithRetryCount_ShouldReturnNoDiagnostics()
    {
        string yaml = TestFixtures.Load("Steps/StepRetry/WithRetryCount.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().BeEmpty();
    }
}
