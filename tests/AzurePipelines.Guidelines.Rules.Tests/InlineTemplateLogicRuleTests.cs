using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Parsing;
using AzurePipelines.Guidelines.Rules.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests;

public sealed class InlineTemplateLogicRuleTests
{
    private static readonly YamlPipelineParser _parser = new();
    private static readonly InlineTemplateLogicRule _rule = new();

    private static async Task<IReadOnlyList<Diagnostic>> EvaluateAsync(string yaml, string filePath = "pipeline.yml")
    {
        PipelineDocument doc = _parser.Parse(yaml, filePath);
        return await _rule.EvaluateAsync(doc).ToListAsync();
    }

    [Fact]
    public void GuidelineId_ShouldBeADOG_GENERAL_006()
    {
        _rule.GuidelineId.Value.Should().Be("ADOG-GENERAL-006");
    }

    [Fact]
    public async Task EvaluateAsync_GivenInlineLogicWithoutTemplateReference_ShouldReturnOneDiagnostic()
    {
        // Arrange
        string yaml = TestFixtures.Load("InlineTemplateLogic/InlineLogic.yml");

        // Act
        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        // Assert
        diagnostics.Should().ContainSingle();
        diagnostics[0].GuidelineId.Value.Should().Be("ADOG-GENERAL-006");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Warning);
    }

    [Fact]
    public async Task EvaluateAsync_GivenReusableTemplateReference_ShouldReturnNoDiagnostics()
    {
        // Arrange
        string yaml = TestFixtures.Load("InlineTemplateLogic/UsesTemplate.yml");

        // Act
        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        // Assert
        diagnostics.Should().BeEmpty();
    }
}
