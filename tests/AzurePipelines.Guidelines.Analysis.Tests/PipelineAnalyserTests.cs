using AzurePipelines.Guidelines.Core;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace AzurePipelines.Guidelines.Analysis.Tests;

public sealed class PipelineAnalyserTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static PipelineDocument EmptyDocument(string filePath = "pipeline.yml") =>
        new(filePath, string.Empty, [], [], [], [], []);

    private static IGuidelineRule MakeRule(string id, params Diagnostic[] diagnostics)
    {
        IGuidelineRule rule = Substitute.For<IGuidelineRule>();
        rule.GuidelineId.Returns(new GuidelineId(id));
        rule.EvaluateAsync(Arg.Any<PipelineDocument>(), Arg.Any<CancellationToken>())
            .Returns(diagnostics.ToAsyncEnumerable());
        return rule;
    }

    private static Diagnostic MakeDiagnostic(
        string id,
        DiagnosticSeverity severity = DiagnosticSeverity.Warning) =>
        new(new GuidelineId(id), severity, "msg", "pipeline.yml", null, null);

    // ── Constructor ───────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_GivenNullRules_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new PipelineAnalyser(null!, NullLogger<PipelineAnalyser>.Instance);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_GivenNullLogger_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new PipelineAnalyser([], null!);

        act.Should().Throw<ArgumentNullException>();
    }

    // ── AnalyseAsync: argument validation ─────────────────────────────────────

    [Fact]
    public async Task AnalyseAsync_GivenNullDocument_ShouldThrowArgumentNullException()
    {
        PipelineAnalyser sut = new([], NullLogger<PipelineAnalyser>.Instance);

        Func<Task> act = async () => await sut.AnalyseAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ── AnalyseAsync: no rules ────────────────────────────────────────────────

    [Fact]
    public async Task AnalyseAsync_GivenNoRules_ShouldReturnCleanResult()
    {
        PipelineAnalyser sut = new([], NullLogger<PipelineAnalyser>.Instance);
        PipelineDocument doc = EmptyDocument();

        AnalysisResult result = await sut.AnalyseAsync(doc);

        result.IsClean.Should().BeTrue();
        result.Document.Should().BeSameAs(doc);
        result.Diagnostics.Should().BeEmpty();
    }

    // ── AnalyseAsync: single rule ─────────────────────────────────────────────

    [Fact]
    public async Task AnalyseAsync_GivenOneRuleWithNoDiagnostics_ShouldReturnCleanResult()
    {
        IGuidelineRule rule = MakeRule("ADOG-STEPS-001");
        PipelineAnalyser sut = new([rule], NullLogger<PipelineAnalyser>.Instance);

        AnalysisResult result = await sut.AnalyseAsync(EmptyDocument());

        result.IsClean.Should().BeTrue();
    }

    [Fact]
    public async Task AnalyseAsync_GivenOneRuleWithOneDiagnostic_ShouldReturnThatDiagnostic()
    {
        Diagnostic expected = MakeDiagnostic("ADOG-STEPS-001");
        IGuidelineRule rule = MakeRule("ADOG-STEPS-001", expected);
        PipelineAnalyser sut = new([rule], NullLogger<PipelineAnalyser>.Instance);

        AnalysisResult result = await sut.AnalyseAsync(EmptyDocument());

        result.Diagnostics.Should().ContainSingle().Which.Should().Be(expected);
    }

    // ── AnalyseAsync: multiple rules ──────────────────────────────────────────

    [Fact]
    public async Task AnalyseAsync_GivenTwoRules_ShouldAggregateAllDiagnostics()
    {
        Diagnostic d1 = MakeDiagnostic("ADOG-STEPS-001");
        Diagnostic d2 = MakeDiagnostic("ADOG-JOBS-006", DiagnosticSeverity.Error);
        IGuidelineRule rule1 = MakeRule("ADOG-STEPS-001", d1);
        IGuidelineRule rule2 = MakeRule("ADOG-JOBS-006", d2);
        PipelineAnalyser sut = new([rule1, rule2], NullLogger<PipelineAnalyser>.Instance);

        AnalysisResult result = await sut.AnalyseAsync(EmptyDocument());

        result.Diagnostics.Should().HaveCount(2).And.Contain([d1, d2]);
    }

    // ── AnalyseAsync: MinimumSeverity filtering ───────────────────────────────

    [Fact]
    public async Task AnalyseAsync_GivenMinimumSeverityWarning_ShouldExcludeInfoDiagnostics()
    {
        Diagnostic infoD = MakeDiagnostic("ADOG-GENERAL-001", DiagnosticSeverity.Info);
        Diagnostic warnD = MakeDiagnostic("ADOG-STEPS-001", DiagnosticSeverity.Warning);
        IGuidelineRule rule = MakeRule("ADOG-GENERAL-001", infoD, warnD);
        PipelineAnalyser sut = new([rule], NullLogger<PipelineAnalyser>.Instance);
        AnalysisOptions options = new(MinimumSeverity: DiagnosticSeverity.Warning);

        AnalysisResult result = await sut.AnalyseAsync(EmptyDocument(), options);

        result.Diagnostics.Should().ContainSingle().Which.Should().Be(warnD);
    }

    // ── AnalyseAsync: IncludedGuidelineIds filtering ──────────────────────────

    [Fact]
    public async Task AnalyseAsync_GivenIncludedGuidelineIds_ShouldOnlyRunMatchingRules()
    {
        Diagnostic d1 = MakeDiagnostic("ADOG-STEPS-001");
        Diagnostic d2 = MakeDiagnostic("ADOG-JOBS-006");
        IGuidelineRule rule1 = MakeRule("ADOG-STEPS-001", d1);
        IGuidelineRule rule2 = MakeRule("ADOG-JOBS-006", d2);
        PipelineAnalyser sut = new([rule1, rule2], NullLogger<PipelineAnalyser>.Instance);
        AnalysisOptions options = new(IncludedGuidelineIds: [new GuidelineId("ADOG-STEPS-001")]);

        AnalysisResult result = await sut.AnalyseAsync(EmptyDocument(), options);

        result.Diagnostics.Should().ContainSingle().Which.Should().Be(d1);
    }

    // ── AnalyseAsync: cancellation ────────────────────────────────────────────

    [Fact]
    public async Task AnalyseAsync_GivenCancelledToken_ShouldThrowOperationCanceledException()
    {
        IGuidelineRule rule = MakeRule("ADOG-STEPS-001");
        PipelineAnalyser sut = new([rule], NullLogger<PipelineAnalyser>.Instance);
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        Func<Task> act = async () => await sut.AnalyseAsync(EmptyDocument(), cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
