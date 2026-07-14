using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Parsing;
using AzurePipelines.Guidelines.Rules.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests;

/// <summary>Tests detection of non-relative template paths.</summary>
public sealed class RelativeTemplatePathRuleTests
{
    private static readonly YamlPipelineParser _parser = new();
    private static readonly RelativeTemplatePathRule _rule = new();

    private static async Task<IReadOnlyList<Diagnostic>> EvaluateAsync(string yaml, string filePath = "pipeline.yml")
    {
        PipelineDocument doc = _parser.Parse(yaml, filePath);
        return await _rule.EvaluateAsync(doc).ToListAsync();
    }

    // ── GuidelineId ───────────────────────────────────────────────────────────

    [Fact]
    public void GuidelineId_ShouldBeADOG_GENERAL_001()
    {
        _rule.GuidelineId.Value.Should().Be("ADOG-GENERAL-001");
    }

    // ── Violations ────────────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_GivenRelativeTemplatePath_ShouldReturnOneDiagnostic()
    {
        string yaml = TestFixtures.Load("RelativeTemplatePath/WithRelativePath.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().ContainSingle();
        diagnostics[0].GuidelineId.Value.Should().Be("ADOG-GENERAL-001");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Info);
    }

    // ── No violations ─────────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_GivenAbsoluteTemplatePath_ShouldReturnNoDiagnostics()
    {
        string yaml = TestFixtures.Load("RelativeTemplatePath/WithAbsolutePath.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_GivenCrossRepoTemplateRef_ShouldReturnNoDiagnostics()
    {
        string yaml = TestFixtures.Load("RelativeTemplatePath/WithCrossRepoRef.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().BeEmpty();
    }
}
