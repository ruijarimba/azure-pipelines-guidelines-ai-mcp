using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Parsing;
using AzurePipelines.Guidelines.Rules.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests;

public sealed class StepMissingTimeoutRuleTests
{
    private static readonly YamlPipelineParser _parser = new();
    private static readonly StepMissingTimeoutRule _rule = new();

    private static async Task<IReadOnlyList<Diagnostic>> EvaluateAsync(string yaml, string filePath = "pipeline.yml")
    {
        PipelineDocument doc = _parser.Parse(yaml, filePath);
        return await _rule.EvaluateAsync(doc).ToListAsync();
    }

    // ── GuidelineId ───────────────────────────────────────────────────────────

    [Fact]
    public void GuidelineId_ShouldBeADOG_STEPS_006()
    {
        _rule.GuidelineId.Value.Should().Be("ADOG-STEPS-006");
    }

    // ── Violations ────────────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_GivenTaskStepWithNoTimeout_ShouldReturnOneDiagnostic()
    {
        string yaml = TestFixtures.Load("StepMissingTimeout/TaskWithoutTimeout.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().ContainSingle();
        diagnostics[0].GuidelineId.Value.Should().Be("ADOG-STEPS-006");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Info);
    }

    // ── No violations ─────────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_GivenTaskStepWithTimeout_ShouldReturnNoDiagnostics()
    {
        string yaml = TestFixtures.Load("StepMissingTimeout/TaskWithTimeout.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_GivenScriptStepWithNoTimeout_ShouldReturnNoDiagnostics()
    {
        string yaml = TestFixtures.Load("StepMissingTimeout/ScriptStepNoTimeout.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().BeEmpty();
    }
}
