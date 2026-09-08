using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Parsing;
using AzurePipelines.Guidelines.Rules.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests;

public sealed class JobLevelVariableRuleTests
{
    private static readonly YamlPipelineParser _parser = new();
    private static readonly JobLevelVariableRule _rule = new();

    private static async Task<IReadOnlyList<Diagnostic>> EvaluateAsync(string yaml, string filePath = "pipeline.yml")
    {
        PipelineDocument doc = _parser.Parse(yaml, filePath);
        return await _rule.EvaluateAsync(doc).ToListAsync();
    }

    [Fact]
    public void GuidelineId_ShouldBeADOG_JOBS_003()
    {
        _rule.GuidelineId.Value.Should().Be("ADOG-JOBS-003");
    }

    [Fact]
    public async Task EvaluateAsync_GivenRootVariableWithJobs_ShouldReturnOneDiagnostic()
    {
        string yaml = TestFixtures.Load("JobLevelVariable/WithRootVariable.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().ContainSingle();
        diagnostics[0].GuidelineId.Value.Should().Be("ADOG-JOBS-003");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Info);
    }

    [Fact]
    public async Task EvaluateAsync_GivenNoJobs_ShouldReturnNoDiagnostics()
    {
        string yaml = TestFixtures.Load("JobLevelVariable/WithoutJobs.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().BeEmpty();
    }
}
