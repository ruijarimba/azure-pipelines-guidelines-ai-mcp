using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Parsing;
using AzurePipelines.Guidelines.Rules.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests;

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
        string yaml = TestFixtures.Load("StepRetry/WithoutRetryCount.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().ContainSingle();
        diagnostics[0].GuidelineId.Value.Should().Be("ADOG-STEPS-005");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Info);
    }

    [Fact]
    public async Task EvaluateAsync_GivenStepWithRetryCount_ShouldReturnNoDiagnostics()
    {
        string yaml = TestFixtures.Load("StepRetry/WithRetryCount.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().BeEmpty();
    }
}
