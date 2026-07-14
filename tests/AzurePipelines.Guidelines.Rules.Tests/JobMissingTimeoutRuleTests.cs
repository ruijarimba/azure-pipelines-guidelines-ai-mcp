using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Parsing;
using AzurePipelines.Guidelines.Rules.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests;

/// <summary>Tests detection of jobs without a timeout.</summary>
public sealed class JobMissingTimeoutRuleTests
{
    private static readonly YamlPipelineParser _parser = new();
    private static readonly JobMissingTimeoutRule _rule = new();

    private static async Task<IReadOnlyList<Diagnostic>> EvaluateAsync(string yaml, string filePath = "pipeline.yml")
    {
        PipelineDocument doc = _parser.Parse(yaml, filePath);
        return await _rule.EvaluateAsync(doc).ToListAsync();
    }

    // ── GuidelineId ───────────────────────────────────────────────────────────

    [Fact]
    public void GuidelineId_ShouldBeADOG_JOBS_006()
    {
        _rule.GuidelineId.Value.Should().Be("ADOG-JOBS-006");
    }

    // ── Violations ────────────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_GivenJobWithNoTimeout_ShouldReturnOneDiagnostic()
    {
        string yaml = TestFixtures.Load("JobMissingTimeout/WithoutTimeout.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().ContainSingle();
        diagnostics[0].GuidelineId.Value.Should().Be("ADOG-JOBS-006");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task EvaluateAsync_GivenMultipleJobsOnlyOneWithTimeout_ShouldReturnOneDiagnostic()
    {
        string yaml = TestFixtures.Load("JobMissingTimeout/MultipleJobsMixedTimeout.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().ContainSingle();
        diagnostics[0].Message.Should().Contain("Build");
    }

    [Fact]
    public async Task EvaluateAsync_GivenUnnamedJob_ShouldReturnDiagnosticWithUnnamedPlaceholder()
    {
        string yaml = TestFixtures.Load("JobMissingTimeout/UnnamedJob.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().ContainSingle();
        diagnostics[0].Message.Should().Contain("(unnamed)");
    }

    // ── No violations ─────────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_GivenJobWithTimeout_ShouldReturnNoDiagnostics()
    {
        string yaml = TestFixtures.Load("JobMissingTimeout/WithTimeout.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().BeEmpty();
    }
}
