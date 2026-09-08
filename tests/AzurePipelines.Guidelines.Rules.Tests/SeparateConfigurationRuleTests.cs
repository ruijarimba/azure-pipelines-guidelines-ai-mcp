using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Parsing;
using AzurePipelines.Guidelines.Rules.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests;

public sealed class SeparateConfigurationRuleTests
{
    private static readonly YamlPipelineParser _parser = new();
    private static readonly SeparateConfigurationRule _rule = new();

    private static async Task<IReadOnlyList<Diagnostic>> EvaluateAsync(string yaml, string filePath = "pipeline.yml")
    {
        PipelineDocument doc = _parser.Parse(yaml, filePath);
        return await _rule.EvaluateAsync(doc).ToListAsync();
    }

    [Fact]
    public void GuidelineId_ShouldBeADOG_VARIABLES_004()
    {
        _rule.GuidelineId.Value.Should().Be("ADOG-VARIABLES-004");
    }

    [Fact]
    public async Task EvaluateAsync_GivenEmbeddedConfiguration_ShouldReturnOneDiagnostic()
    {
        string yaml = TestFixtures.Load("SeparateConfiguration/WithEmbeddedConfiguration.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().ContainSingle();
        diagnostics[0].GuidelineId.Value.Should().Be("ADOG-VARIABLES-004");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task EvaluateAsync_GivenVariableTemplateConfiguration_ShouldReturnNoDiagnostics()
    {
        string yaml = TestFixtures.Load("SeparateConfiguration/WithVariableTemplate.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().BeEmpty();
    }
}
