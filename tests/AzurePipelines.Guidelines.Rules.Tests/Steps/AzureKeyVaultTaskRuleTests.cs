using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Parsing;
using AzurePipelines.Guidelines.Rules.Tests.Fixtures;
using AzurePipelines.Guidelines.Rules.Steps;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests.Steps;

public sealed class AzureKeyVaultTaskRuleTests
{
    private static readonly YamlPipelineParser _parser = new();
    private static readonly AzureKeyVaultTaskRule _rule = new();

    private static async Task<IReadOnlyList<Diagnostic>> EvaluateAsync(string yaml, string filePath = "pipeline.yml")
    {
        PipelineDocument doc = _parser.Parse(yaml, filePath);
        return await _rule.EvaluateAsync(doc).ToListAsync();
    }

    // ── GuidelineId ───────────────────────────────────────────────────────────

    [Fact]
    public void GuidelineId_ShouldBeADOG_STEPS_011()
    {
        _rule.GuidelineId.Value.Should().Be("ADOG-STEPS-011");
    }

    // ── Violations ────────────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_GivenAzureKeyVaultTask_ShouldReturnOneDiagnostic()
    {
        string yaml = TestFixtures.Load("Steps/AzureKeyVaultTask/WithKeyVaultTask.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().ContainSingle();
        diagnostics[0].GuidelineId.Value.Should().Be("ADOG-STEPS-011");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Error);
    }

    // ── No violations ─────────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_GivenNoKeyVaultTask_ShouldReturnNoDiagnostics()
    {
        string yaml = TestFixtures.Load("Steps/AzureKeyVaultTask/WithoutKeyVaultTask.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().BeEmpty();
    }
}
