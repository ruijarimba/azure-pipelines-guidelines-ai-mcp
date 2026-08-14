using System.Text.Json;
using AzurePipelines.Guidelines.Analysis;
using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Mcp.Tools;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AzurePipelines.Guidelines.Mcp.Tests.Tools;

public sealed class PipelineAnalysisToolsTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static PipelineAnalysisTools MakeSut(
        IPipelineParser? parser = null,
        IPipelineAnalyser? analyser = null,
        PipelinePathResolver? pathResolver = null,
        IGuidelineRepository? guidelineRepository = null) =>
        new(
            parser ?? Substitute.For<IPipelineParser>(),
            analyser ?? Substitute.For<IPipelineAnalyser>(),
            pathResolver ?? new PipelinePathResolver(),
            guidelineRepository ?? Substitute.For<IGuidelineRepository>());

    private static T Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json)!;

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "adog-mcp-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static PipelineDocument EmptyDocument() =>
        new(
            Jobs: [],
            Stages: [],
            Steps: [],
            Variables: [],
            Parameters: [],
            RawContent: string.Empty,
            FilePath: "(inline)");

    private static AnalysisResult MakeResult(IReadOnlyList<Diagnostic>? diagnostics = null) =>
        new(EmptyDocument(), diagnostics ?? []);

    private static Diagnostic MakeDiagnostic(
        string id = "ADOG-STEPS-001",
        DiagnosticSeverity severity = DiagnosticSeverity.Error,
        string message = "Test message",
        int? line = 5) =>
        new(
            new GuidelineId(id),
            severity,
            message,
            FilePath: "(inline)",
            Line: line,
            Column: null);

    // ── Target validation ──────────────────────────────────────────────────────

    [Fact]
    public async Task AnalyzeTemplateAsync_GivenNoTarget_ShouldReturnErrorResponse()
    {
        // Arrange
        PipelineAnalysisTools sut = MakeSut();

        // Act
        string result = await sut.AnalyzeTemplateAsync();

        // Assert
        JsonElement obj = Deserialize<JsonElement>(result);
        obj.GetProperty("error").GetString().Should().Contain("exactly one");
    }

    [Fact]
    public async Task AnalyzeTemplateAsync_GivenBothTargets_ShouldReturnErrorResponse()
    {
        // Arrange
        PipelineAnalysisTools sut = MakeSut();

        // Act
        string result = await sut.AnalyzeTemplateAsync("steps: []", "pipeline.yml");

        // Assert
        JsonElement obj = Deserialize<JsonElement>(result);
        obj.GetProperty("error").GetString().Should().Contain("exactly one");
    }

    // ── Parsing failure ───────────────────────────────────────────────────────

    [Fact]
    public async Task AnalyzeTemplateAsync_GivenInvalidYaml_ShouldReturnParsingErrorResponse()
    {
        // Arrange
        IPipelineParser parser = Substitute.For<IPipelineParser>();
        parser.Parse(Arg.Any<string>(), Arg.Any<string>())
              .Returns(_ => throw new PipelineParsingException("Invalid YAML structure."));
        PipelineAnalysisTools sut = MakeSut(parser: parser);

        // Act
        string result = await sut.AnalyzeTemplateAsync(yaml: "not: valid: yaml: !!!");

        // Assert
        JsonElement obj = Deserialize<JsonElement>(result);
        obj.GetProperty("error").GetString().Should().Contain("Failed to parse YAML");
    }

    // ── Clean pipeline (no violations) ────────────────────────────────────────

    [Fact]
    public async Task AnalyzeTemplateAsync_GivenCleanPipeline_ShouldReturnEmptySummary()
    {
        // Arrange
        IPipelineParser parser = Substitute.For<IPipelineParser>();
        parser.Parse(Arg.Any<string>(), Arg.Any<string>()).Returns(EmptyDocument());

        IPipelineAnalyser analyser = Substitute.For<IPipelineAnalyser>();
        analyser.AnalyseAsync(Arg.Any<PipelineDocument>(), Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>())
                .Returns(MakeResult([]));

        PipelineAnalysisTools sut = MakeSut(parser, analyser);

        // Act
        string result = await sut.AnalyzeTemplateAsync(yaml: "steps: []");

        // Assert
        JsonElement summary = Deserialize<JsonElement>(result).GetProperty("summary");
        summary.GetProperty("filesAnalyzed").GetInt32().Should().Be(1);
        summary.GetProperty("filesWithFindings").GetInt32().Should().Be(0);
        summary.GetProperty("totalFindings").GetInt32().Should().Be(0);
        summary.TryGetProperty("byRecommendation", out _).Should().BeFalse();
    }

    // ── Pipeline with violations ──────────────────────────────────────────────

    [Fact]
    public async Task AnalyzeTemplateAsync_GivenViolation_ShouldReturnDiagnosticInResult()
    {
        // Arrange
        IPipelineParser parser = Substitute.For<IPipelineParser>();
        parser.Parse(Arg.Any<string>(), Arg.Any<string>()).Returns(EmptyDocument());

        Diagnostic diag = MakeDiagnostic("ADOG-STEPS-001", DiagnosticSeverity.Error, "Use templates.", line: 7);
        IPipelineAnalyser analyser = Substitute.For<IPipelineAnalyser>();
        analyser.AnalyseAsync(Arg.Any<PipelineDocument>(), Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>())
                .Returns(MakeResult([diag]));

        IGuidelineRepository guidelineRepository = Substitute.For<IGuidelineRepository>();
        guidelineRepository.FindById(new GuidelineId("ADOG-STEPS-001"))
            .Returns(new GuidelineDefinition(
                new GuidelineId("ADOG-STEPS-001"), GuidelineCategory.Steps, GuidelineSeverity.Do,
                "Test", "Test", null, [], [], null, []));
        PipelineAnalysisTools sut = MakeSut(parser, analyser, guidelineRepository: guidelineRepository);

        // Act
        string result = await sut.AnalyzeTemplateAsync(yaml: "steps: []");

        // Assert
        JsonElement obj = Deserialize<JsonElement>(result);
        JsonElement summary = obj.GetProperty("summary");
        summary.GetProperty("totalFindings").GetInt32().Should().Be(1);
        summary.GetProperty("byRecommendation").GetProperty("do").GetInt32().Should().Be(1);
        summary.GetProperty("byCategory").GetProperty("steps").GetInt32().Should().Be(1);
        summary.GetProperty("byRule").GetProperty("ADOG-STEPS-001").GetInt32().Should().Be(1);

        JsonElement item = obj.GetProperty("diagnostics")[0];
        item.GetProperty("ruleId").GetString().Should().Be("ADOG-STEPS-001");
        item.GetProperty("recommendation").GetString().Should().Be("do");
        item.GetProperty("message").GetString().Should().Be("Use templates.");
        item.GetProperty("line").GetInt32().Should().Be(7);
    }

    [Fact]
    public async Task AnalyzeTemplateAsync_GivenMultipleViolations_ShouldReturnAllDiagnostics()
    {
        // Arrange
        IPipelineParser parser = Substitute.For<IPipelineParser>();
        parser.Parse(Arg.Any<string>(), Arg.Any<string>()).Returns(EmptyDocument());

        IPipelineAnalyser analyser = Substitute.For<IPipelineAnalyser>();
        analyser.AnalyseAsync(Arg.Any<PipelineDocument>(), Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>())
                .Returns(MakeResult([
                    MakeDiagnostic("ADOG-STEPS-001", line: 1),
                    MakeDiagnostic("ADOG-JOBS-006", DiagnosticSeverity.Warning, line: 10),
                ]));

        PipelineAnalysisTools sut = MakeSut(parser, analyser);

        // Act
        string result = await sut.AnalyzeTemplateAsync(yaml: "steps: []");

        // Assert
        JsonElement obj = Deserialize<JsonElement>(result);
        obj.GetProperty("summary").GetProperty("totalFindings").GetInt32().Should().Be(2);
        JsonElement[] items = obj.GetProperty("diagnostics").EnumerateArray().ToArray();
        items.Should().HaveCount(2);
        items[0].GetProperty("ruleId").GetString().Should().Be("ADOG-STEPS-001");
        items[1].GetProperty("ruleId").GetString().Should().Be("ADOG-JOBS-006");
    }

    // ── guidelineIds filter ───────────────────────────────────────────────────

    [Fact]
    public async Task AnalyzeTemplateAsync_GivenGuidelineIdFilter_ShouldPassOptionsToAnalyser()
    {
        // Arrange
        IPipelineParser parser = Substitute.For<IPipelineParser>();
        parser.Parse(Arg.Any<string>(), Arg.Any<string>()).Returns(EmptyDocument());

        IPipelineAnalyser analyser = Substitute.For<IPipelineAnalyser>();
        analyser.AnalyseAsync(Arg.Any<PipelineDocument>(), Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>())
                .Returns(MakeResult([]));

        PipelineAnalysisTools sut = MakeSut(parser, analyser);

        // Act
        await sut.AnalyzeTemplateAsync(yaml: "steps: []", guidelineIds: "ADOG-STEPS-001,ADOG-JOBS-006");

        // Assert
        await analyser.Received(1).AnalyseAsync(
            Arg.Any<PipelineDocument>(),
            Arg.Is<AnalysisOptions>(o =>
                o.IncludedGuidelineIds != null &&
                o.IncludedGuidelineIds.Count == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnalyzeTemplateAsync_GivenNullGuidelineIds_ShouldPassDefaultOptions()
    {
        // Arrange
        IPipelineParser parser = Substitute.For<IPipelineParser>();
        parser.Parse(Arg.Any<string>(), Arg.Any<string>()).Returns(EmptyDocument());

        IPipelineAnalyser analyser = Substitute.For<IPipelineAnalyser>();
        analyser.AnalyseAsync(Arg.Any<PipelineDocument>(), Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>())
                .Returns(MakeResult([]));

        PipelineAnalysisTools sut = MakeSut(parser, analyser);

        // Act
        await sut.AnalyzeTemplateAsync(yaml: "steps: []", guidelineIds: null);

        // Assert
        await analyser.Received(1).AnalyseAsync(
            Arg.Any<PipelineDocument>(),
            Arg.Is<AnalysisOptions>(o => o.IncludedGuidelineIds == null),
            Arg.Any<CancellationToken>());
    }

    // ── Severity serialisation ────────────────────────────────────────────────

    // ── Recommendation serialisation ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(GuidelineSeverity.Do,       "do")]
    [InlineData(GuidelineSeverity.DoNot,    "donot")]
    [InlineData(GuidelineSeverity.Avoid,    "avoid")]
    [InlineData(GuidelineSeverity.Consider, "consider")]
    public async Task AnalyzeTemplateAsync_RecommendationValues_ShouldBeGuidelineSeverityInOutput(
        GuidelineSeverity guidelineSeverity, string expectedJsonValue)
    {
        // Arrange
        IPipelineParser parser = Substitute.For<IPipelineParser>();
        parser.Parse(Arg.Any<string>(), Arg.Any<string>()).Returns(EmptyDocument());

        Diagnostic diag = MakeDiagnostic("ADOG-STEPS-001");
        IPipelineAnalyser analyser = Substitute.For<IPipelineAnalyser>();
        analyser.AnalyseAsync(Arg.Any<PipelineDocument>(), Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>())
                .Returns(MakeResult([diag]));

        IGuidelineRepository guidelineRepository = Substitute.For<IGuidelineRepository>();
        guidelineRepository.FindById(new GuidelineId("ADOG-STEPS-001"))
            .Returns(new GuidelineDefinition(
                new GuidelineId("ADOG-STEPS-001"), GuidelineCategory.Steps, guidelineSeverity,
                "Test", "Test", null, [], [], null, []));
        PipelineAnalysisTools sut = MakeSut(parser, analyser, guidelineRepository: guidelineRepository);

        // Act
        string result = await sut.AnalyzeTemplateAsync(yaml: "steps: []");

        // Assert
        JsonElement item = Deserialize<JsonElement>(result).GetProperty("diagnostics")[0];
        item.GetProperty("recommendation").GetString().Should().Be(expectedJsonValue);
    }

    // ── Null line number ──────────────────────────────────────────────────────

    [Fact]
    public async Task AnalyzeTemplateAsync_GivenNullLine_ShouldOmitLineFromJson()
    {
        // Arrange
        IPipelineParser parser = Substitute.For<IPipelineParser>();
        parser.Parse(Arg.Any<string>(), Arg.Any<string>()).Returns(EmptyDocument());

        Diagnostic diag = MakeDiagnostic(line: null);
        IPipelineAnalyser analyser = Substitute.For<IPipelineAnalyser>();
        analyser.AnalyseAsync(Arg.Any<PipelineDocument>(), Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>())
                .Returns(MakeResult([diag]));

        PipelineAnalysisTools sut = MakeSut(parser, analyser);

        // Act
        string result = await sut.AnalyzeTemplateAsync(yaml: "steps: []");

        // Assert
        JsonElement item = Deserialize<JsonElement>(result).GetProperty("diagnostics")[0];
        item.TryGetProperty("line", out _).Should().BeFalse();
    }

    // ── GuidelineId filter: malformed IDs are silently skipped ───────────────

    [Fact]
    public async Task AnalyzeTemplateAsync_GivenOneMalformedAndOneValidGuidelineId_ShouldFilterOnlyValidId()
    {
        // Arrange
        IPipelineParser parser = Substitute.For<IPipelineParser>();
        parser.Parse(Arg.Any<string>(), Arg.Any<string>()).Returns(EmptyDocument());

        Diagnostic validDiag = MakeDiagnostic("ADOG-STEPS-001");
        Diagnostic otherDiag = MakeDiagnostic("ADOG-STEPS-002");
        IPipelineAnalyser analyser = Substitute.For<IPipelineAnalyser>();
        analyser.AnalyseAsync(
                Arg.Any<PipelineDocument>(),
                Arg.Is<AnalysisOptions>(o =>
                    o.IncludedGuidelineIds != null &&
                    o.IncludedGuidelineIds.Count == 1),
                Arg.Any<CancellationToken>())
                .Returns(MakeResult([validDiag]));

        PipelineAnalysisTools sut = MakeSut(parser, analyser);

        // Act — "NOTVALID" is malformed and should be skipped; only ADOG-STEPS-001 survives
        string result = await sut.AnalyzeTemplateAsync(yaml: "steps: []", guidelineIds: "ADOG-STEPS-001, NOTVALID");

        // Assert
        JsonElement obj = Deserialize<JsonElement>(result);
        obj.GetProperty("diagnostics").GetArrayLength().Should().Be(1);
        obj.GetProperty("diagnostics")[0].GetProperty("ruleId").GetString().Should().Be("ADOG-STEPS-001");
    }

    [Fact]
    public async Task AnalyzeTemplateAsync_GivenAllMalformedGuidelineIds_ShouldUseDefaultOptions()
    {
        // Arrange
        IPipelineParser parser = Substitute.For<IPipelineParser>();
        parser.Parse(Arg.Any<string>(), Arg.Any<string>()).Returns(EmptyDocument());

        Diagnostic diag = MakeDiagnostic("ADOG-STEPS-001");
        IPipelineAnalyser analyser = Substitute.For<IPipelineAnalyser>();
        analyser.AnalyseAsync(
                Arg.Any<PipelineDocument>(),
                Arg.Is<AnalysisOptions>(o => o.IncludedGuidelineIds == null),
                Arg.Any<CancellationToken>())
                .Returns(MakeResult([diag]));

        PipelineAnalysisTools sut = MakeSut(parser, analyser);

        // Act — all IDs are malformed; parser should fall back to AnalysisOptions.Default
        string result = await sut.AnalyzeTemplateAsync(yaml: "steps: []", guidelineIds: "NOTVALID, ALSOBAD");

        // Assert
        JsonElement obj = Deserialize<JsonElement>(result);
        obj.GetProperty("diagnostics").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task AnalyzeTemplateAsync_GivenDirectoryInput_ShouldReturnFileResults()
    {
        // Arrange
        IPipelineParser parser = Substitute.For<IPipelineParser>();
        parser.Parse(Arg.Any<string>(), Arg.Any<string>()).Returns(EmptyDocument());

        IPipelineAnalyser analyser = Substitute.For<IPipelineAnalyser>();
        analyser.AnalyseAsync(Arg.Any<PipelineDocument>(), Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>())
                .Returns(MakeResult([MakeDiagnostic()]));

        PipelineAnalysisTools sut = MakeSut(parser, analyser, new PipelinePathResolver());
        string tempDirectory = CreateTempDirectory();
        string nestedDirectory = Path.Combine(tempDirectory, "nested");
        Directory.CreateDirectory(nestedDirectory);
        await File.WriteAllTextAsync(Path.Combine(tempDirectory, "one.yml"), "steps: []");
        await File.WriteAllTextAsync(Path.Combine(nestedDirectory, "two.yaml"), "steps: []");

        try
        {
            // Act
            string result = await sut.AnalyzeTemplateAsync(fileOrPath: tempDirectory);

            // Assert
            JsonElement obj = Deserialize<JsonElement>(result);
            obj.GetProperty("summary").GetProperty("filesAnalyzed").GetInt32().Should().Be(2);
            obj.GetProperty("summary").GetProperty("filesWithFindings").GetInt32().Should().Be(2);
            JsonElement[] items = obj.GetProperty("files").EnumerateArray().ToArray();
            items.Should().HaveCount(2);
            items[0].GetProperty("filePath").GetString().Should().NotBeNull();
            items[1].GetProperty("filePath").GetString().Should().NotBeNull();
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    // ── category filter ─────────────────────────────────────────────────────

    [Fact]
    public async Task AnalyzeTemplateAsync_GivenValidCategory_ShouldPassIncludedCategoriesToAnalyser()
    {
        // Arrange
        IPipelineParser parser = Substitute.For<IPipelineParser>();
        parser.Parse(Arg.Any<string>(), Arg.Any<string>()).Returns(EmptyDocument());

        IPipelineAnalyser analyser = Substitute.For<IPipelineAnalyser>();
        analyser.AnalyseAsync(Arg.Any<PipelineDocument>(), Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>())
                .Returns(MakeResult([]));

        PipelineAnalysisTools sut = MakeSut(parser, analyser);

        // Act
        await sut.AnalyzeTemplateAsync(yaml: "steps: []", category: "steps");

        // Assert
        await analyser.Received(1).AnalyseAsync(
            Arg.Any<PipelineDocument>(),
            Arg.Is<AnalysisOptions>(o =>
                o.IncludedCategories != null &&
                o.IncludedCategories.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnalyzeTemplateAsync_GivenUnknownCategory_ShouldReturnErrorResponse()
    {
        // Arrange
        PipelineAnalysisTools sut = MakeSut();

        // Act
        string result = await sut.AnalyzeTemplateAsync(yaml: "steps: []", category: "not-a-category");

        // Assert
        JsonElement obj = Deserialize<JsonElement>(result);
        obj.GetProperty("error").GetString().Should().Contain("not-a-category");
    }

    [Fact]
    public async Task AnalyzeTemplateAsync_GivenValidCategoryOnDirectory_ShouldPassIncludedCategoriesToAnalyser()
    {
        // Arrange
        IPipelineParser parser = Substitute.For<IPipelineParser>();
        parser.Parse(Arg.Any<string>(), Arg.Any<string>()).Returns(EmptyDocument());

        IPipelineAnalyser analyser = Substitute.For<IPipelineAnalyser>();
        analyser.AnalyseAsync(Arg.Any<PipelineDocument>(), Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>())
                .Returns(MakeResult([]));

        string tempDirectory = CreateTempDirectory();
        await File.WriteAllTextAsync(Path.Combine(tempDirectory, "pipeline.yml"), "steps: []");

        try
        {
            PipelineAnalysisTools sut = MakeSut(parser, analyser, new PipelinePathResolver());

            // Act
            await sut.AnalyzeTemplateAsync(fileOrPath: tempDirectory, category: "jobs");

            // Assert
            await analyser.Received(1).AnalyseAsync(
                Arg.Any<PipelineDocument>(),
                Arg.Is<AnalysisOptions>(o =>
                    o.IncludedCategories != null &&
                    o.IncludedCategories.Count == 1),
                Arg.Any<CancellationToken>());
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task AnalyzeTemplateAsync_GivenUnknownCategoryOnDirectory_ShouldReturnErrorResponse()
    {
        // Arrange
        string tempDirectory = CreateTempDirectory();
        await File.WriteAllTextAsync(Path.Combine(tempDirectory, "pipeline.yml"), "steps: []");

        try
        {
            PipelineAnalysisTools sut = MakeSut(pathResolver: new PipelinePathResolver());

            // Act
            string result = await sut.AnalyzeTemplateAsync(fileOrPath: tempDirectory, category: "not-a-category");

            // Assert
            JsonElement obj = Deserialize<JsonElement>(result);
            obj.GetProperty("error").GetString().Should().Contain("not-a-category");
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }
}
