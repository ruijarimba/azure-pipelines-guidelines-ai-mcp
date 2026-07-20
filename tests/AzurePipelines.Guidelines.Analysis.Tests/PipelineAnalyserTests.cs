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

    private static IGuidelineRepository NullRepository() =>
        Substitute.For<IGuidelineRepository>();

    private static IPipelineSchemaValidator NullSchemaValidator() =>
        Substitute.For<IPipelineSchemaValidator>();

    private static IGuidelineAutomationMetadataProvider AutomationMetadataProvider(
        GuidelineAutomationStatus status = GuidelineAutomationStatus.Enforceable)
    {
        IGuidelineAutomationMetadataProvider provider = Substitute.For<IGuidelineAutomationMetadataProvider>();
        provider.GetAutomationMetadata(Arg.Any<GuidelineId>())
            .Returns(new GuidelineAutomationMetadata(status, "Test automation metadata."));
        return provider;
    }

    private static PipelineAnalyser CreateAnalyser(
        IEnumerable<IGuidelineRule> rules,
        IGuidelineRepository? repository = null,
        IGuidelineAutomationMetadataProvider? automationMetadataProvider = null,
        IPipelineSchemaValidator? schemaValidator = null) =>
        new(
            rules,
            repository ?? NullRepository(),
            automationMetadataProvider ?? AutomationMetadataProvider(),
            schemaValidator ?? NullSchemaValidator(),
            NullLogger<PipelineAnalyser>.Instance);

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
        Action act = () => _ = new PipelineAnalyser(
            null!, NullRepository(), AutomationMetadataProvider(), NullSchemaValidator(), NullLogger<PipelineAnalyser>.Instance);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_GivenNullLogger_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new PipelineAnalyser(
            [], NullRepository(), AutomationMetadataProvider(), NullSchemaValidator(), null!);

        act.Should().Throw<ArgumentNullException>();
    }

    // ── AnalyseAsync: argument validation ─────────────────────────────────────

    [Fact]
    public async Task AnalyseAsync_GivenNullDocument_ShouldThrowArgumentNullException()
    {
        PipelineAnalyser sut = CreateAnalyser([]);

        Func<Task> act = async () => await sut.AnalyseAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ── AnalyseAsync: no rules ────────────────────────────────────────────────

    [Fact]
    public async Task AnalyseAsync_GivenNoRules_ShouldReturnCleanResult()
    {
        PipelineAnalyser sut = CreateAnalyser([]);
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
        PipelineAnalyser sut = CreateAnalyser([rule]);

        AnalysisResult result = await sut.AnalyseAsync(EmptyDocument());

        result.IsClean.Should().BeTrue();
    }

    [Fact]
    public async Task AnalyseAsync_GivenOneRuleWithOneDiagnostic_ShouldReturnThatDiagnostic()
    {
        Diagnostic expected = MakeDiagnostic("ADOG-STEPS-001");
        IGuidelineRule rule = MakeRule("ADOG-STEPS-001", expected);
        PipelineAnalyser sut = CreateAnalyser([rule]);

        AnalysisResult result = await sut.AnalyseAsync(EmptyDocument());

        result.Diagnostics.Should().ContainSingle().Which.Should().Be(expected);
    }

    [Fact]
    public async Task AnalyseAsync_ShouldReturnSchemaDiagnosticsAndStillRunRules()
    {
        Diagnostic expected = MakeDiagnostic("ADOG-STEPS-001");
        IGuidelineRule rule = MakeRule("ADOG-STEPS-001", expected);
        IPipelineSchemaValidator schemaValidator = Substitute.For<IPipelineSchemaValidator>();
        SchemaDiagnostic schemaDiagnostic = new("ADOG-SCHEMA-005", "Unknown property.", 1);
        schemaValidator.Validate(Arg.Any<string>(), Arg.Any<string>(), PipelineSchemaContext.Pipeline)
            .Returns([schemaDiagnostic]);
        PipelineAnalyser sut = CreateAnalyser([rule], schemaValidator: schemaValidator);

        AnalysisResult result = await sut.AnalyseAsync(EmptyDocument());

        result.Diagnostics.Should().ContainSingle().Which.Should().Be(expected);
        result.StructuralDiagnostics.Should().ContainSingle().Which.Should().Be(schemaDiagnostic);
        result.IsClean.Should().BeFalse();
        _ = rule.Received(1).EvaluateAsync(Arg.Any<PipelineDocument>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnalyseAsync_GivenUnnamedJob_ShouldReturnSchemaAndRuleDiagnostics()
    {
        const string yaml = """
            jobs:
              - steps:
                  - script: echo building
            """;
        PipelineDocument document = new("UnnamedJob.yml", yaml, [], [], [],
            [new JobNode(null, null, null, [], [], null, 2)], []);
        Diagnostic ruleDiagnostic = MakeDiagnostic("ADOG-JOBS-006");
        IGuidelineRule rule = MakeRule("ADOG-JOBS-006", ruleDiagnostic);
        IPipelineSchemaValidator schemaValidator = Substitute.For<IPipelineSchemaValidator>();
        SchemaDiagnostic schemaDiagnostic = new("ADOG-SCHEMA-010", "An item must specify a job, deployment, or template.", 2);
        schemaValidator.Validate(yaml, "UnnamedJob.yml", PipelineSchemaContext.Pipeline)
            .Returns([schemaDiagnostic]);
        PipelineAnalyser sut = CreateAnalyser([rule], schemaValidator: schemaValidator);

        AnalysisResult result = await sut.AnalyseAsync(document);

        result.StructuralDiagnostics.Should().ContainSingle().Which.Code.Should().Be("ADOG-SCHEMA-010");
        result.Diagnostics.Should().ContainSingle().Which.GuidelineId.Value.Should().Be("ADOG-JOBS-006");
    }

    // ── AnalyseAsync: multiple rules ──────────────────────────────────────────

    [Fact]
    public async Task AnalyseAsync_GivenTwoRules_ShouldAggregateAllDiagnostics()
    {
        Diagnostic d1 = MakeDiagnostic("ADOG-STEPS-001");
        Diagnostic d2 = MakeDiagnostic("ADOG-JOBS-006", DiagnosticSeverity.Error);
        IGuidelineRule rule1 = MakeRule("ADOG-STEPS-001", d1);
        IGuidelineRule rule2 = MakeRule("ADOG-JOBS-006", d2);
        PipelineAnalyser sut = CreateAnalyser([rule1, rule2]);

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
        PipelineAnalyser sut = CreateAnalyser([rule]);
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
        PipelineAnalyser sut = CreateAnalyser([rule1, rule2]);
        AnalysisOptions options = new(IncludedGuidelineIds: [new GuidelineId("ADOG-STEPS-001")]);

        AnalysisResult result = await sut.AnalyseAsync(EmptyDocument(), options);

        result.Diagnostics.Should().ContainSingle().Which.Should().Be(d1);
    }

    // ── AnalyseAsync: cancellation ────────────────────────────────────────────

    [Fact]
    public async Task AnalyseAsync_GivenCancelledToken_ShouldThrowOperationCanceledException()
    {
        IGuidelineRule rule = MakeRule("ADOG-STEPS-001");
        PipelineAnalyser sut = CreateAnalyser([rule]);
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        Func<Task> act = async () => await sut.AnalyseAsync(EmptyDocument(), cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    // ── AnalyseAsync: IncludedCategories filtering ──────────────────────────────

    [Fact]
    public async Task AnalyseAsync_GivenIncludedCategories_ShouldOnlyRunRulesInMatchingCategory()
    {
        Diagnostic d1 = MakeDiagnostic("ADOG-STEPS-001");
        Diagnostic d2 = MakeDiagnostic("ADOG-JOBS-006");
        IGuidelineRule stepsRule = MakeRule("ADOG-STEPS-001", d1);
        IGuidelineRule jobsRule  = MakeRule("ADOG-JOBS-006",  d2);

        IGuidelineRepository repository = Substitute.For<IGuidelineRepository>();
        repository.FindById(new GuidelineId("ADOG-STEPS-001"))
            .Returns(new GuidelineDefinition(
                new GuidelineId("ADOG-STEPS-001"),
                GuidelineCategory.Steps,
                GuidelineSeverity.Avoid,
                "Title", "Desc",
                Rationale: null, Tags: [], DetectionHints: [], Fix: null, References: []));
        repository.FindById(new GuidelineId("ADOG-JOBS-006"))
            .Returns(new GuidelineDefinition(
                new GuidelineId("ADOG-JOBS-006"),
                GuidelineCategory.Jobs,
                GuidelineSeverity.Do,
                "Title", "Desc",
                Rationale: null, Tags: [], DetectionHints: [], Fix: null, References: []));

        PipelineAnalyser sut = CreateAnalyser([stepsRule, jobsRule], repository);

        AnalysisOptions options = new(IncludedCategories: [GuidelineCategory.Steps]);

        AnalysisResult result = await sut.AnalyseAsync(EmptyDocument(), options);

        result.Diagnostics.Should().ContainSingle().Which.Should().Be(d1);
    }

    [Fact]
    public async Task AnalyseAsync_GivenHeuristicRuleAndDefaultOptions_ShouldSkipTheRule()
    {
        IGuidelineRule rule = MakeRule("ADOG-GENERAL-001", MakeDiagnostic("ADOG-GENERAL-001"));
        PipelineAnalyser sut = CreateAnalyser(
            [rule],
            automationMetadataProvider: AutomationMetadataProvider(GuidelineAutomationStatus.Heuristic));

        AnalysisResult result = await sut.AnalyseAsync(EmptyDocument());

        result.Diagnostics.Should().BeEmpty();
        result.SkippedRuleDetails.Should().ContainSingle().Which.Should().Match<SkippedGuideline>(
            skipped => skipped.Id.Value == "ADOG-GENERAL-001" &&
                skipped.Status == GuidelineAutomationStatus.Heuristic);
        _ = rule.DidNotReceive().EvaluateAsync(Arg.Any<PipelineDocument>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnalyseAsync_GivenHeuristicRuleAndOptIn_ShouldEvaluateTheRule()
    {
        Diagnostic expected = MakeDiagnostic("ADOG-GENERAL-001");
        IGuidelineRule rule = MakeRule("ADOG-GENERAL-001", expected);
        PipelineAnalyser sut = CreateAnalyser(
            [rule],
            automationMetadataProvider: AutomationMetadataProvider(GuidelineAutomationStatus.Heuristic));

        AnalysisResult result = await sut.AnalyseAsync(EmptyDocument(), new AnalysisOptions(IncludeHeuristics: true));

        result.Diagnostics.Should().ContainSingle().Which.Should().Be(expected);
        result.SkippedRuleDetails.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyseAsync_GivenNotAutomatableRule_ShouldSkipTheRuleEvenWhenHeuristicsAreIncluded()
    {
        IGuidelineRule rule = MakeRule("ADOG-STEPS-008", MakeDiagnostic("ADOG-STEPS-008"));
        PipelineAnalyser sut = CreateAnalyser(
            [rule],
            automationMetadataProvider: AutomationMetadataProvider(GuidelineAutomationStatus.NotAutomatable));

        AnalysisResult result = await sut.AnalyseAsync(EmptyDocument(), new AnalysisOptions(IncludeHeuristics: true));

        result.Diagnostics.Should().BeEmpty();
        result.SkippedRuleDetails.Should().ContainSingle().Which.Status.Should().Be(GuidelineAutomationStatus.NotAutomatable);
        _ = rule.DidNotReceive().EvaluateAsync(Arg.Any<PipelineDocument>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnalyseAsync_GivenRuleWithoutAutomationMetadata_ShouldNotEvaluateTheRule()
    {
        // Arrange
        IGuidelineRule rule = MakeRule("ADOG-STEPS-001", MakeDiagnostic("ADOG-STEPS-001"));
        IGuidelineAutomationMetadataProvider provider = Substitute.For<IGuidelineAutomationMetadataProvider>();
        provider.GetAutomationMetadata(Arg.Any<GuidelineId>()).Returns((GuidelineAutomationMetadata?)null);
        PipelineAnalyser sut = CreateAnalyser([rule], automationMetadataProvider: provider);

        // Act
        AnalysisResult result = await sut.AnalyseAsync(EmptyDocument(), new AnalysisOptions(IncludeHeuristics: true));

        // Assert
        result.Diagnostics.Should().BeEmpty();
        result.SkippedRuleDetails.Should().BeEmpty();
        _ = rule.DidNotReceive().EvaluateAsync(Arg.Any<PipelineDocument>(), Arg.Any<CancellationToken>());
    }
}
