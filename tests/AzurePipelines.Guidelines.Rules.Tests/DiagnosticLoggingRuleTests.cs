using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Parsing;
using AzurePipelines.Guidelines.Rules.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests;

public sealed class DiagnosticLoggingRuleTests
{
    private static readonly YamlPipelineParser _parser = new();
    private static readonly DiagnosticLoggingRule _rule = new();

    private static async Task<IReadOnlyList<Diagnostic>> EvaluateAsync(string yaml, string filePath = "pipeline.yml")
    {
        PipelineDocument doc = _parser.Parse(yaml, filePath);
        return await _rule.EvaluateAsync(doc).ToListAsync();
    }

    [Fact]
    public void GuidelineId_ShouldBeADOG_STEPS_003()
    {
        // Arrange
        _rule.GuidelineId.Value.Should().Be("ADOG-STEPS-003");
    }

    [Fact]
    public async Task EvaluateAsync_GivenScriptWithoutLogging_ShouldReturnOneDiagnostic()
    {
        // Arrange
        string yaml = TestFixtures.Load("DiagnosticLogging/WithoutLogging.yml");

        // Act
        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        // Assert
        diagnostics.Should().ContainSingle();
        diagnostics[0].GuidelineId.Value.Should().Be("ADOG-STEPS-003");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Warning);
    }

    [Fact]
    public async Task EvaluateAsync_GivenScriptWithLogging_ShouldReturnNoDiagnostics()
    {
        // Arrange
        string yaml = TestFixtures.Load("DiagnosticLogging/WithLogging.yml");

        // Act
        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        // Assert
        diagnostics.Should().BeEmpty();
    }
}
