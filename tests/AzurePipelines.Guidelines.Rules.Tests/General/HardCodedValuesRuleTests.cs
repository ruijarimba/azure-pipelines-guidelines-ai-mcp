using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Parsing;
using AzurePipelines.Guidelines.Rules.Tests.Fixtures;
using AzurePipelines.Guidelines.Rules.General;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests.General;

public sealed class HardCodedValuesRuleTests
{
    private static readonly YamlPipelineParser _parser = new();
    private static readonly HardCodedValuesRule _rule = new();

    private static async Task<IReadOnlyList<Diagnostic>> EvaluateAsync(string yaml, string filePath = "pipeline.yml")
    {
        PipelineDocument doc = _parser.Parse(yaml, filePath);
        return await _rule.EvaluateAsync(doc).ToListAsync();
    }

    [Fact]
    public void GuidelineId_ShouldBeADOG_GENERAL_007()
    {
        _rule.GuidelineId.Value.Should().Be("ADOG-GENERAL-007");
    }

    [Fact]
    public async Task EvaluateAsync_GivenHardCodedValue_ShouldReturnOneDiagnostic()
    {
        string yaml = TestFixtures.Load("General/HardCodedValues/HardCodedValue.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().ContainSingle();
        diagnostics[0].GuidelineId.Value.Should().Be("ADOG-GENERAL-007");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task EvaluateAsync_GivenParameterizedValue_ShouldReturnNoDiagnostics()
    {
        string yaml = TestFixtures.Load("General/HardCodedValues/ParameterizedValue.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().BeEmpty();
    }
}
