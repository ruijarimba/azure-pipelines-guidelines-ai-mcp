using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Parsing;
using AzurePipelines.Guidelines.Rules.Tests.Fixtures;
using AzurePipelines.Guidelines.Rules.Jobs;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests.Jobs;

/// <summary>Tests detection of jobs that compose multiple step templates.</summary>
public sealed class MultipleStepsTemplatesInJobRuleTests
{
    private static readonly YamlPipelineParser _parser = new();
    private static readonly MultipleStepsTemplatesInJobRule _rule = new();

    private static async Task<IReadOnlyList<Diagnostic>> EvaluateAsync(string yaml, string filePath = "pipeline.yml")
    {
        PipelineDocument doc = _parser.Parse(yaml, filePath);
        return await _rule.EvaluateAsync(doc).ToListAsync();
    }

    [Fact]
    public void GuidelineId_ShouldBeADOG_JOBS_002()
    {
        _rule.GuidelineId.Value.Should().Be("ADOG-JOBS-002");
    }

    [Fact]
    public async Task EvaluateAsync_GivenJobWithMultipleStepsTemplates_ShouldReturnOneDiagnostic()
    {
        string yaml = TestFixtures.Load("Jobs/MultipleStepsTemplatesInJob/MultipleTemplates.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().ContainSingle();
        diagnostics[0].GuidelineId.Value.Should().Be("ADOG-JOBS-002");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Info);
    }

    [Fact]
    public async Task EvaluateAsync_GivenJobWithSingleStepsTemplate_ShouldReturnNoDiagnostics()
    {
        string yaml = TestFixtures.Load("Jobs/MultipleStepsTemplatesInJob/SingleTemplate.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().BeEmpty();
    }
}
