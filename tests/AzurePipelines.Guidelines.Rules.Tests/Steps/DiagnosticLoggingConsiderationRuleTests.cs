using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Parsing;
using AzurePipelines.Guidelines.Rules.Tests.Fixtures;
using AzurePipelines.Guidelines.Rules.Steps;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests.Steps;

public sealed class DiagnosticLoggingConsiderationRuleTests
{
    private static readonly YamlPipelineParser _parser = new();
    private static readonly DiagnosticLoggingConsiderationRule _rule = new();

    private static async Task<IReadOnlyList<Diagnostic>> EvaluateAsync(string yaml, string filePath = "pipeline.yml")
    {
        PipelineDocument doc = _parser.Parse(yaml, filePath);
        return await _rule.EvaluateAsync(doc).ToListAsync();
    }

    [Fact]
    public void GuidelineId_ShouldBeADOG_STEPS_004()
    {
        _rule.GuidelineId.Value.Should().Be("ADOG-STEPS-004");
    }

    [Fact]
    public async Task EvaluateAsync_GivenScriptWithoutLogging_ShouldReturnOneDiagnostic()
    {
        string yaml = TestFixtures.Load("Steps/DiagnosticLogging/WithoutLogging.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().ContainSingle();
        diagnostics[0].GuidelineId.Value.Should().Be("ADOG-STEPS-004");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Warning);
    }

    [Fact]
    public async Task EvaluateAsync_GivenScriptWithLogging_ShouldReturnNoDiagnostics()
    {
        string yaml = TestFixtures.Load("Steps/DiagnosticLogging/WithLogging.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().BeEmpty();
    }
}
