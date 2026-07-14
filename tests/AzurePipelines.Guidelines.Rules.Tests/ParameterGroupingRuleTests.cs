using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Parsing;
using AzurePipelines.Guidelines.Rules.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests;

/// <summary>Tests detection of parameters that are not grouped clearly.</summary>
public sealed class ParameterGroupingRuleTests
{
    private static readonly YamlPipelineParser _parser = new();
    private static readonly ParameterGroupingRule _rule = new();

    private static async Task<IReadOnlyList<Diagnostic>> EvaluateAsync(string yaml, string filePath = "pipeline.yml")
    {
        PipelineDocument doc = _parser.Parse(yaml, filePath);
        return await _rule.EvaluateAsync(doc).ToListAsync();
    }

    [Fact]
    public void GuidelineId_ShouldBeADOG_PARAMETERS_001()
    {
        _rule.GuidelineId.Value.Should().Be("ADOG-PARAMETERS-001");
    }

    [Fact]
    public async Task EvaluateAsync_GivenSensitiveParameter_ShouldReturnOneDiagnostic()
    {
        string yaml = TestFixtures.Load("ParameterGrouping/SensitiveParameter.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().ContainSingle();
        diagnostics[0].GuidelineId.Value.Should().Be("ADOG-PARAMETERS-001");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Info);
    }

    [Fact]
    public async Task EvaluateAsync_GivenNonSensitiveParameter_ShouldReturnNoDiagnostics()
    {
        string yaml = TestFixtures.Load("ParameterGrouping/NonSensitiveParameter.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().BeEmpty();
    }
}
