using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Parsing;
using AzurePipelines.Guidelines.Rules.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests;

public sealed class RunIndependentStagesInParallelRuleTests
{
    private static readonly YamlPipelineParser _parser = new();
    private static readonly RunIndependentStagesInParallelRule _rule = new();

    private static async Task<IReadOnlyList<Diagnostic>> EvaluateAsync(string yaml, string filePath = "pipeline.yml")
    {
        PipelineDocument doc = _parser.Parse(yaml, filePath);
        return await _rule.EvaluateAsync(doc).ToListAsync();
    }

    [Fact]
    public void GuidelineId_ShouldBeADOG_STAGES_002()
    {
        _rule.GuidelineId.Value.Should().Be("ADOG-STAGES-002");
    }

    [Fact]
    public async Task EvaluateAsync_GivenMultipleStagesWithoutDependsOn_ShouldReturnOneDiagnostic()
    {
        string yaml = TestFixtures.Load("RunIndependentStagesInParallel/WithoutDependsOn.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().ContainSingle();
        diagnostics[0].GuidelineId.Value.Should().Be("ADOG-STAGES-002");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task EvaluateAsync_GivenMultipleStagesWithDependsOn_ShouldReturnNoDiagnostics()
    {
        string yaml = TestFixtures.Load("RunIndependentStagesInParallel/WithDependsOn.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().BeEmpty();
    }
}
