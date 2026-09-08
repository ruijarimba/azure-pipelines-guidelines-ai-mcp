using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Parsing;
using AzurePipelines.Guidelines.Rules.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests;

public sealed class ParameterMissingValuesRuleTests
{
    private static readonly YamlPipelineParser _parser = new();
    private static readonly ParameterMissingValuesRule _rule = new();

    private static async Task<IReadOnlyList<Diagnostic>> EvaluateAsync(string yaml, string filePath = "pipeline.yml")
    {
        PipelineDocument doc = _parser.Parse(yaml, filePath);
        return await _rule.EvaluateAsync(doc).ToListAsync();
    }

    // ── GuidelineId ───────────────────────────────────────────────────────────

    [Fact]
    public void GuidelineId_ShouldBeADOG_PARAMETERS_002()
    {
        _rule.GuidelineId.Value.Should().Be("ADOG-PARAMETERS-002");
    }

    // ── Violations ────────────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_GivenStringParameterWithNoValues_ShouldReturnOneDiagnostic()
    {
        string yaml = TestFixtures.Load("ParameterMissingValues/WithoutValues.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().ContainSingle();
        diagnostics[0].GuidelineId.Value.Should().Be("ADOG-PARAMETERS-002");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Error);
        diagnostics[0].Message.Should().Contain("environment");
    }

    // ── No violations ─────────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_GivenStringParameterWithValues_ShouldReturnNoDiagnostics()
    {
        string yaml = TestFixtures.Load("ParameterMissingValues/WithValues.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_GivenNonStringParameter_ShouldReturnNoDiagnostics()
    {
        string yaml = TestFixtures.Load("ParameterMissingValues/NonStringType.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_GivenImplicitStringTypeParameterWithNoValues_ShouldReturnOneDiagnostic()
    {
        string yaml = TestFixtures.Load("ParameterMissingValues/ImplicitStringType.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().ContainSingle();
        diagnostics[0].GuidelineId.Value.Should().Be("ADOG-PARAMETERS-002");
        diagnostics[0].Message.Should().Contain("environment");
    }
}
