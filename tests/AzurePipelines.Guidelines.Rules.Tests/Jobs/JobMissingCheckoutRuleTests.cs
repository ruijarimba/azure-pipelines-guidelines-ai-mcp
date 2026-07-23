using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Parsing;
using AzurePipelines.Guidelines.Rules.Tests.Fixtures;
using AzurePipelines.Guidelines.Rules.Jobs;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests.Jobs;

/// <summary>Tests detection of jobs that do not check out repository contents.</summary>
public sealed class JobMissingCheckoutRuleTests
{
    private static readonly YamlPipelineParser _parser = new();
    private static readonly JobMissingCheckoutRule _rule = new();

    private static async Task<IReadOnlyList<Diagnostic>> EvaluateAsync(string yaml, string filePath = "pipeline.yml")
    {
        PipelineDocument doc = _parser.Parse(yaml, filePath);
        return await _rule.EvaluateAsync(doc).ToListAsync();
    }

    // ── GuidelineId ───────────────────────────────────────────────────────────

    [Fact]
    public void GuidelineId_ShouldBeADOG_JOBS_001()
    {
        _rule.GuidelineId.Value.Should().Be("ADOG-JOBS-001");
    }

    // ── Violations ────────────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_GivenJobWithNoCheckout_ShouldReturnOneDiagnostic()
    {
        string yaml = TestFixtures.Load("Jobs/JobMissingCheckout/WithoutCheckout.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().ContainSingle();
        diagnostics[0].GuidelineId.Value.Should().Be("ADOG-JOBS-001");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Info);
    }

    // ── No violations ─────────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_GivenJobWithCheckoutSelf_ShouldReturnNoDiagnostics()
    {
        string yaml = TestFixtures.Load("Jobs/JobMissingCheckout/WithCheckout.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_GivenJobWithCheckoutNone_ShouldReturnNoDiagnostics()
    {
        string yaml = TestFixtures.Load("Jobs/JobMissingCheckout/WithCheckoutNone.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_GivenJobWithArbitraryCheckoutRepository_ShouldReturnNoDiagnostics()
    {
        const string yaml = """
        jobs:
        - job: Build
          steps:
          - checkout: sourceRepository
          - checkout: anotherRepository
        """;

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_GivenJobWithEmptyCheckoutValue_ShouldReturnOneDiagnostic()
    {
        const string yaml = """
        jobs:
        - job: Build
          steps:
          - checkout:
        """;

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().ContainSingle();
    }
}
