using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Parsing;
using AzurePipelines.Guidelines.Rules.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests;

/// <summary>Tests detection of variables that resemble secrets.</summary>
public sealed class SecretLikeVariableRuleTests
{
    private static readonly YamlPipelineParser _parser = new();
    private static readonly SecretLikeVariableRule _rule = new();

    private static async Task<IReadOnlyList<Diagnostic>> EvaluateAsync(string yaml, string filePath = "pipeline.yml")
    {
        PipelineDocument doc = _parser.Parse(yaml, filePath);
        return await _rule.EvaluateAsync(doc).ToListAsync();
    }

    // ── GuidelineId ───────────────────────────────────────────────────────────

    [Fact]
    public void GuidelineId_ShouldBeADOG_VARIABLES_003()
    {
        _rule.GuidelineId.Value.Should().Be("ADOG-VARIABLES-003");
    }

    // ── Violations ────────────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_GivenSecretLikeVariableName_ShouldReturnDiagnostic()
    {
        string yaml = TestFixtures.Load("SecretLikeVariable/WithSecretLikeName.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().ContainSingle();
        diagnostics[0].GuidelineId.Value.Should().Be("ADOG-VARIABLES-003");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task EvaluateAsync_GivenMappingStyleSecretVariable_ShouldReturnDiagnostic()
    {
        string yaml = TestFixtures.Load("SecretLikeVariable/WithMappingStyleSecretName.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().ContainSingle();
        diagnostics[0].GuidelineId.Value.Should().Be("ADOG-VARIABLES-003");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Error);
    }

    // ── No violations

    [Fact]
    public async Task EvaluateAsync_GivenSafeVariableName_ShouldReturnNoDiagnostics()
    {
        string yaml = TestFixtures.Load("SecretLikeVariable/WithSafeName.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().BeEmpty();
    }
}
