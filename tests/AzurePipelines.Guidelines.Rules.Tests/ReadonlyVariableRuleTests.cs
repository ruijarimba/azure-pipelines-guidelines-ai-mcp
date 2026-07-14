using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Parsing;
using AzurePipelines.Guidelines.Rules.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests;

/// <summary>Tests detection of variables that should be read-only.</summary>
public sealed class ReadonlyVariableRuleTests
{
    private static readonly YamlPipelineParser _parser = new();
    private static readonly ReadonlyVariableRule _rule = new();

    private static async Task<IReadOnlyList<Diagnostic>> EvaluateAsync(string yaml, string filePath = "pipeline.yml")
    {
        PipelineDocument doc = _parser.Parse(yaml, filePath);
        return await _rule.EvaluateAsync(doc).ToListAsync();
    }

    [Fact]
    public void GuidelineId_ShouldBeADOG_VARIABLES_001()
    {
        _rule.GuidelineId.Value.Should().Be("ADOG-VARIABLES-001");
    }

    [Fact]
    public async Task EvaluateAsync_GivenMutableVariable_ShouldReturnOneDiagnostic()
    {
        string yaml = TestFixtures.Load("ReadonlyVariable/MutableVariable.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().ContainSingle();
        diagnostics[0].GuidelineId.Value.Should().Be("ADOG-VARIABLES-001");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Info);
    }

    [Fact]
    public async Task EvaluateAsync_GivenReadonlyVariable_ShouldReturnNoDiagnostics()
    {
        string yaml = TestFixtures.Load("ReadonlyVariable/ReadonlyVariable.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().BeEmpty();
    }
}
