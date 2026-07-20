using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Parsing;
using AzurePipelines.Guidelines.Rules.Tests.Fixtures;
using AzurePipelines.Guidelines.Rules.Stages;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests.Stages;

/// <summary>Tests grouping related jobs into explicit stages.</summary>
public sealed class UseStagesForRelatedJobsRuleTests
{
    private static readonly YamlPipelineParser _parser = new();
    private static readonly UseStagesForRelatedJobsRule _rule = new();

    private static async Task<IReadOnlyList<Diagnostic>> EvaluateAsync(string yaml, string filePath = "pipeline.yml")
    {
        PipelineDocument doc = _parser.Parse(yaml, filePath);
        return await _rule.EvaluateAsync(doc).ToListAsync();
    }

    [Fact]
    public void GuidelineId_ShouldBeADOG_STAGES_001()
    {
        _rule.GuidelineId.Value.Should().Be("ADOG-STAGES-001");
    }

    [Fact]
    public async Task EvaluateAsync_GivenMultipleTopLevelJobs_ShouldReturnOneDiagnostic()
    {
        string yaml = TestFixtures.Load("Stages/UseStagesForRelatedJobs/MultipleTopLevelJobs.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().ContainSingle();
        diagnostics[0].GuidelineId.Value.Should().Be("ADOG-STAGES-001");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Info);
    }

    [Fact]
    public async Task EvaluateAsync_GivenSingleTopLevelJob_ShouldReturnNoDiagnostics()
    {
        string yaml = TestFixtures.Load("Stages/UseStagesForRelatedJobs/SingleTopLevelJob.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().BeEmpty();
    }
}
