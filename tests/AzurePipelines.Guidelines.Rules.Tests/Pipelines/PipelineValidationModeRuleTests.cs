using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Parsing;
using AzurePipelines.Guidelines.Rules.Tests.Fixtures;
using AzurePipelines.Guidelines.Rules.Pipelines;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests.Pipelines;

/// <summary>Tests detection of pipeline validation mode configuration issues.</summary>
public sealed class PipelineValidationModeRuleTests
{
    private static readonly YamlPipelineParser _parser = new();
    private static readonly PipelineValidationModeRule _rule = new();

    private static async Task<IReadOnlyList<Diagnostic>> EvaluateAsync(string yaml, string filePath = "pipeline.yml")
    {
        PipelineDocument doc = _parser.Parse(yaml, filePath);
        return await _rule.EvaluateAsync(doc).ToListAsync();
    }

    [Fact]
    public void GuidelineId_ShouldBeADOG_PIPELINES_001()
    {
        _rule.GuidelineId.Value.Should().Be("ADOG-PIPELINES-001");
    }

    [Fact]
    public async Task EvaluateAsync_GivenPipelineWithoutValidationMode_ShouldReturnOneDiagnostic()
    {
        string yaml = TestFixtures.Load("Pipelines/PipelineValidationMode/WithoutValidationMode.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().ContainSingle();
        diagnostics[0].GuidelineId.Value.Should().Be("ADOG-PIPELINES-001");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Info);
    }

    [Fact]
    public async Task EvaluateAsync_GivenPipelineWithValidationModeHint_ShouldReturnNoDiagnostics()
    {
        string yaml = TestFixtures.Load("Pipelines/PipelineValidationMode/WithValidationMode.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().BeEmpty();
    }
}
