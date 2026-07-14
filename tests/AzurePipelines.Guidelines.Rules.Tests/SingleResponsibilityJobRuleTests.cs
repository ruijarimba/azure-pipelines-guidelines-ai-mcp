using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Parsing;
using AzurePipelines.Guidelines.Rules.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests;

/// <summary>Tests detection of jobs with multiple unrelated responsibilities.</summary>
public sealed class SingleResponsibilityJobRuleTests
{
    private static readonly YamlPipelineParser _parser = new();
    private static readonly SingleResponsibilityJobRule _rule = new();

    private static async Task<IReadOnlyList<Diagnostic>> EvaluateAsync(string yaml, string filePath = "pipeline.yml")
    {
        PipelineDocument doc = _parser.Parse(yaml, filePath);
        return await _rule.EvaluateAsync(doc).ToListAsync();
    }

    [Fact]
    public void GuidelineId_ShouldBeADOG_JOBS_008()
    {
        _rule.GuidelineId.Value.Should().Be("ADOG-JOBS-008");
    }

    [Fact]
    public async Task EvaluateAsync_GivenMultiResponsibilityJob_ShouldReturnOneDiagnostic()
    {
        string yaml = TestFixtures.Load("SingleResponsibility/WithMultipleResponsibilities.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().ContainSingle();
        diagnostics[0].GuidelineId.Value.Should().Be("ADOG-JOBS-008");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task EvaluateAsync_GivenSingleResponsibilityJob_ShouldReturnNoDiagnostics()
    {
        string yaml = TestFixtures.Load("SingleResponsibility/WithSingleResponsibility.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().BeEmpty();
    }
}
