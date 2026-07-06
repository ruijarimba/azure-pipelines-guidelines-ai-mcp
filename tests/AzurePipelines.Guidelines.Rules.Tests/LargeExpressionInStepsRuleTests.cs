using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Parsing;
using AzurePipelines.Guidelines.Rules.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests;

public sealed class LargeExpressionInStepsRuleTests
{
    private static readonly YamlPipelineParser _parser = new();
    private static readonly LargeExpressionInStepsRule _rule = new();

    private static async Task<IReadOnlyList<Diagnostic>> EvaluateAsync(string yaml, string filePath = "pipeline.yml")
    {
        PipelineDocument doc = _parser.Parse(yaml, filePath);
        return await _rule.EvaluateAsync(doc).ToListAsync();
    }

    // ── GuidelineId ───────────────────────────────────────────────────────────

    [Fact]
    public void GuidelineId_ShouldBeADOG_STEPS_010()
    {
        _rule.GuidelineId.Value.Should().Be("ADOG-STEPS-010");
    }

    // ── Violations ────────────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_GivenTemplateExpression_ShouldReturnOneDiagnostic()
    {
        string yaml = TestFixtures.Load("LargeExpression/WithTemplateExpression.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().ContainSingle();
        diagnostics[0].GuidelineId.Value.Should().Be("ADOG-STEPS-010");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Error);
    }

    // ── No violations ─────────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_GivenNoExpressions_ShouldReturnNoDiagnostics()
    {
        string yaml = TestFixtures.Load("LargeExpression/WithNoExpressions.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().BeEmpty();
    }
}
