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
        IGuidelineRepository? repository = null) =>
        new(
            parser ?? Substitute.For<IPipelineParser>(),
            analyser ?? Substitute.For<IPipelineAnalyser>(),
            pathResolver ?? new PipelinePathResolver(),
            repository ?? new GuidelineRepository([]));

    private static T Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json)!;

    private static JsonElement[] GetDiagnostics(string json) =>
        [.. Deserialize<JsonElement>(json).GetProperty("diagnostics").EnumerateArray()];

    private static GuidelineDefinition MakeGuideline(string id) =>
        new(
            new GuidelineId(id),
            GuidelineCategory.Steps,
            GuidelineSeverity.Do,
            "Use templates",
            "Extract repeated pipeline steps into templates.",
            "Templates reduce duplication.",
            Tags: [],
            DetectionHints: [],
            Fix: new FixGuidance("Extract the steps into a template.", "steps: []", "template: steps.yml"),
            References: ["https://learn.microsoft.com/azure/devops/pipelines/process/templates"]);

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

    private static GuidelineDefinition MakeGuideline(
        string id,
        IReadOnlyList<string> references) =>
        MakeGuideline(id) with { References = references };

    // ── Canonical references ──────────────────────────────────────────────────

    [Fact]
    public void CanonicalizeReferences_GivenCanonicalMetadata_ShouldPrependItAndRetainDistinctManifestLinks()
    {
        // Arrange
        GuidelineDefinition guideline = MakeGuideline(
            "ADOG-STEPS-008",
            [
                "https://stale.example.test/steps-008",
                "https://learn.microsoft.com/azure/devops/pipelines/process/templates",
                "https://canonical.example.test/steps-008",
            ]);
        IGuidelineMetadataProvider metadataProvider = Substitute.For<IGuidelineMetadataProvider>();
        metadataProvider.GetCanonicalReference(guideline.Id)
            .Returns("https://canonical.example.test/steps-008");

        // Act
        IReadOnlyList<GuidelineDefinition> result =
            GuidelinesMcpServiceCollectionExtensions.CanonicalizeReferences([guideline], metadataProvider);

        // Assert
        result.Should().ContainSingle();
        result[0].References.Should().Equal(
            "https://canonical.example.test/steps-008",
            "https://stale.example.test/steps-008",
            "https://learn.microsoft.com/azure/devops/pipelines/process/templates");
    }

    [Fact]
    public void CanonicalizeReferences_GivenNoMetadata_ShouldPreserveManifestLinks()
    {
        // Arrange
        GuidelineDefinition guideline = MakeGuideline(
            "ADOG-STEPS-008",
            ["https://manifest.example.test/steps-008"]);
        IGuidelineMetadataProvider metadataProvider = Substitute.For<IGuidelineMetadataProvider>();

        // Act
        IReadOnlyList<GuidelineDefinition> result =
            GuidelinesMcpServiceCollectionExtensions.CanonicalizeReferences([guideline], metadataProvider);

        // Assert
        result[0].References.Should().Equal("https://manifest.example.test/steps-008");
    }

    // ── Null / empty yaml ─────────────────────────────────────────────────────

    [Fact]
    public async Task AnalyzePipelineAsync_GivenNullYaml_ShouldReturnErrorResponse()
    {
        // Arrange
        PipelineAnalysisTools sut = MakeSut();

        // Act
        string result = await sut.AnalyzePipelineAsync(null!);

        // Assert
        JsonElement obj = Deserialize<JsonElement>(result);
        obj.GetProperty("error").GetString().Should().Contain("yaml");
    }

    [Fact]
    public async Task AnalyzePipelineAsync_GivenEmptyYaml_ShouldReturnErrorResponse()
    {
        // Arrange
        PipelineAnalysisTools sut = MakeSut();

        // Act
        string result = await sut.AnalyzePipelineAsync("   ");

        // Assert
        JsonElement obj = Deserialize<JsonElement>(result);
        obj.GetProperty("error").GetString().Should().Contain("yaml");
    }

    // ── Parsing failure ───────────────────────────────────────────────────────

    [Fact]
    public async Task AnalyzePipelineAsync_GivenInvalidYaml_ShouldReturnParsingErrorResponse()
    {
        // Arrange
        IPipelineParser parser = Substitute.For<IPipelineParser>();
        parser.Parse(Arg.Any<string>(), Arg.Any<string>())
              .Returns(_ => throw new PipelineParsingException("Invalid YAML structure."));
        PipelineAnalysisTools sut = MakeSut(parser: parser);

        // Act
        string result = await sut.AnalyzePipelineAsync("not: valid: yaml: !!!");

        // Assert
        JsonElement obj = Deserialize<JsonElement>(result);
        obj.GetProperty("error").GetString().Should().Contain("Failed to parse YAML");
    }

    // ── Clean pipeline (no violations) ────────────────────────────────────────

    [Fact]
    public async Task AnalyzePipelineAsync_GivenCleanPipeline_ShouldReturnEmptyDiagnosticsAndRules()
    {
        // Arrange
        IPipelineParser parser = Substitute.For<IPipelineParser>();
        parser.Parse(Arg.Any<string>(), Arg.Any<string>()).Returns(EmptyDocument());

        IPipelineAnalyser analyser = Substitute.For<IPipelineAnalyser>();
        analyser.AnalyseAsync(Arg.Any<PipelineDocument>(), Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>())
                .Returns(MakeResult([]));

        PipelineAnalysisTools sut = MakeSut(parser, analyser);

        // Act
        string result = await sut.AnalyzePipelineAsync("steps: []");

        // Assert
        JsonElement response = Deserialize<JsonElement>(result);
        response.GetProperty("diagnostics").EnumerateArray().Should().BeEmpty();
        response.GetProperty("rules").EnumerateArray().Should().BeEmpty();
    }

    // ── Pipeline with violations ──────────────────────────────────────────────

    [Fact]
    public async Task AnalyzePipelineAsync_GivenViolation_ShouldReturnDiagnosticInResult()
    {
        // Arrange
        IPipelineParser parser = Substitute.For<IPipelineParser>();
        parser.Parse(Arg.Any<string>(), Arg.Any<string>()).Returns(EmptyDocument());

        Diagnostic diag = MakeDiagnostic("ADOG-STEPS-001", DiagnosticSeverity.Error, "Use templates.", line: 7);
        IPipelineAnalyser analyser = Substitute.For<IPipelineAnalyser>();
        analyser.AnalyseAsync(Arg.Any<PipelineDocument>(), Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>())
                .Returns(MakeResult([diag]));

        PipelineAnalysisTools sut = MakeSut(parser, analyser);

        // Act
        string result = await sut.AnalyzePipelineAsync("steps: []");

        // Assert
        JsonElement[] items = GetDiagnostics(result);
        items.Should().HaveCount(1);

        JsonElement item = items[0];
        item.GetProperty("ruleId").GetString().Should().Be("ADOG-STEPS-001");
        item.GetProperty("severity").GetString().Should().Be("error");
        item.GetProperty("message").GetString().Should().Be("Use templates.");
        item.GetProperty("line").GetInt32().Should().Be(7);
        Deserialize<JsonElement>(result).GetProperty("rules").EnumerateArray().Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzePipelineAsync_GivenRepeatedKnownGuideline_ShouldReturnCompactRuleDetailsOnce()
    {
        // Arrange
        IPipelineParser parser = Substitute.For<IPipelineParser>();
        parser.Parse(Arg.Any<string>(), Arg.Any<string>()).Returns(EmptyDocument());
        IPipelineAnalyser analyser = Substitute.For<IPipelineAnalyser>();
        analyser.AnalyseAsync(Arg.Any<PipelineDocument>(), Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>())
                .Returns(MakeResult([
                    MakeDiagnostic("ADOG-STEPS-001", line: 7),
                    MakeDiagnostic("ADOG-STEPS-001", line: 12),
                ]));
        PipelineAnalysisTools sut = MakeSut(
            parser,
            analyser,
            repository: new GuidelineRepository([MakeGuideline("ADOG-STEPS-001")]));

        // Act
        string result = await sut.AnalyzePipelineAsync("steps: []");

        // Assert
        JsonElement[] rules = [.. Deserialize<JsonElement>(result).GetProperty("rules").EnumerateArray()];
        rules.Should().ContainSingle();
        rules[0].GetProperty("id").GetString().Should().Be("ADOG-STEPS-001");
        rules[0].TryGetProperty("guidance", out _).Should().BeFalse();
        rules[0].GetProperty("references")[0].GetString().Should().StartWith("https://");
        rules[0].TryGetProperty("description", out _).Should().BeFalse();
        rules[0].TryGetProperty("rationale", out _).Should().BeFalse();
        rules[0].TryGetProperty("fix", out _).Should().BeFalse();
    }

    [Fact]
    public async Task AnalyzePipelineAsync_GivenIncludeGuidance_ShouldReturnRuleGuidance()
    {
        // Arrange
        IPipelineParser parser = Substitute.For<IPipelineParser>();
        parser.Parse(Arg.Any<string>(), Arg.Any<string>()).Returns(EmptyDocument());
        IPipelineAnalyser analyser = Substitute.For<IPipelineAnalyser>();
        analyser.AnalyseAsync(Arg.Any<PipelineDocument>(), Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>())
                .Returns(MakeResult([MakeDiagnostic("ADOG-STEPS-001")]));
        PipelineAnalysisTools sut = MakeSut(
            parser,
            analyser,
            repository: new GuidelineRepository([MakeGuideline("ADOG-STEPS-001")]));

        // Act
        string result = await sut.AnalyzePipelineAsync("steps: []", includeGuidance: true);

        // Assert
        JsonElement[] rules = [.. Deserialize<JsonElement>(result).GetProperty("rules").EnumerateArray()];
        rules.Should().ContainSingle();
        rules[0].GetProperty("guidance").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task AnalyzePipelineAsync_GivenMultipleViolations_ShouldReturnAllDiagnostics()
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
        string result = await sut.AnalyzePipelineAsync("steps: []");

        // Assert
        JsonElement[] items = GetDiagnostics(result);
        items.Should().HaveCount(2);
        items[0].GetProperty("ruleId").GetString().Should().Be("ADOG-STEPS-001");
        items[1].GetProperty("ruleId").GetString().Should().Be("ADOG-JOBS-006");
    }

    // ── guidelineIds filter ───────────────────────────────────────────────────

    [Fact]
    public async Task AnalyzePipelineAsync_GivenGuidelineIdFilter_ShouldPassOptionsToAnalyser()
    {
        // Arrange
        IPipelineParser parser = Substitute.For<IPipelineParser>();
        parser.Parse(Arg.Any<string>(), Arg.Any<string>()).Returns(EmptyDocument());

        IPipelineAnalyser analyser = Substitute.For<IPipelineAnalyser>();
        analyser.AnalyseAsync(Arg.Any<PipelineDocument>(), Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>())
                .Returns(MakeResult([]));

        PipelineAnalysisTools sut = MakeSut(parser, analyser);

        // Act
        await sut.AnalyzePipelineAsync("steps: []", guidelineIds: "ADOG-STEPS-001,ADOG-JOBS-006");

        // Assert
        await analyser.Received(1).AnalyseAsync(
            Arg.Any<PipelineDocument>(),
            Arg.Is<AnalysisOptions>(o =>
                o.IncludedGuidelineIds != null &&
                o.IncludedGuidelineIds.Count == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnalyzePipelineAsync_GivenNullGuidelineIds_ShouldPassDefaultOptions()
    {
        // Arrange
        IPipelineParser parser = Substitute.For<IPipelineParser>();
        parser.Parse(Arg.Any<string>(), Arg.Any<string>()).Returns(EmptyDocument());

        IPipelineAnalyser analyser = Substitute.For<IPipelineAnalyser>();
        analyser.AnalyseAsync(Arg.Any<PipelineDocument>(), Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>())
                .Returns(MakeResult([]));

        PipelineAnalysisTools sut = MakeSut(parser, analyser);

        // Act
        await sut.AnalyzePipelineAsync("steps: []", guidelineIds: null);

        // Assert
        await analyser.Received(1).AnalyseAsync(
            Arg.Any<PipelineDocument>(),
            Arg.Is<AnalysisOptions>(o => o.IncludedGuidelineIds == null),
            Arg.Any<CancellationToken>());
    }

    // ── Severity serialisation ────────────────────────────────────────────────

    [Theory]
    [InlineData(DiagnosticSeverity.Error, "error")]
    [InlineData(DiagnosticSeverity.Warning, "warning")]
    [InlineData(DiagnosticSeverity.Info, "info")]
    public async Task AnalyzePipelineAsync_SeverityValues_ShouldBeLowercaseInOutput(
        DiagnosticSeverity severity, string expectedJsonValue)
    {
        // Arrange
        IPipelineParser parser = Substitute.For<IPipelineParser>();
        parser.Parse(Arg.Any<string>(), Arg.Any<string>()).Returns(EmptyDocument());

        IPipelineAnalyser analyser = Substitute.For<IPipelineAnalyser>();
        analyser.AnalyseAsync(Arg.Any<PipelineDocument>(), Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>())
                .Returns(MakeResult([MakeDiagnostic(severity: severity)]));

        PipelineAnalysisTools sut = MakeSut(parser, analyser);

        // Act
        string result = await sut.AnalyzePipelineAsync("steps: []");

        // Assert
        JsonElement[] items = GetDiagnostics(result);
        items[0].GetProperty("severity").GetString().Should().Be(expectedJsonValue);
    }

    // ── Null line number ──────────────────────────────────────────────────────

    [Fact]
    public async Task AnalyzePipelineAsync_GivenNullLine_ShouldOmitLineFromJson()
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
        string result = await sut.AnalyzePipelineAsync("steps: []");

        // Assert
        JsonElement[] items = GetDiagnostics(result);
        items[0].TryGetProperty("line", out _).Should().BeFalse();
    }

    // ── GuidelineId filter: malformed IDs are silently skipped ───────────────

    [Fact]
    public async Task AnalyzePipelineAsync_GivenOneMalformedAndOneValidGuidelineId_ShouldFilterOnlyValidId()
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
        string result = await sut.AnalyzePipelineAsync("steps: []", guidelineIds: "ADOG-STEPS-001, NOTVALID");

        // Assert
        JsonElement[] items = GetDiagnostics(result);
        items.Should().ContainSingle();
        items[0].GetProperty("ruleId").GetString().Should().Be("ADOG-STEPS-001");
    }

    [Fact]
    public async Task AnalyzePipelineAsync_GivenAllMalformedGuidelineIds_ShouldUseDefaultOptions()
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
        string result = await sut.AnalyzePipelineAsync("steps: []", guidelineIds: "NOTVALID, ALSOBAD");

        // Assert
        JsonElement[] items = GetDiagnostics(result);
        items.Should().ContainSingle();
    }

    [Fact]
    public async Task AnalyzePipelinePathsAsync_GivenDirectoryInput_ShouldReturnFileResults()
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
            string result = await sut.AnalyzePipelinePathsAsync([tempDirectory]);

            // Assert
            JsonElement[] items = [.. Deserialize<JsonElement>(result).GetProperty("files").EnumerateArray()];
            items.Should().HaveCount(2);
            items[0].GetProperty("filePath").GetString().Should().NotBeNull();
            items[1].GetProperty("filePath").GetString().Should().NotBeNull();
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task AnalyzePipelinePathsAsync_GivenMarkdownFormat_ShouldReturnCompactLinkedReport()
    {
        // Arrange
        IPipelineParser parser = Substitute.For<IPipelineParser>();
        parser.Parse(Arg.Any<string>(), Arg.Any<string>()).Returns(EmptyDocument());
        IPipelineAnalyser analyser = Substitute.For<IPipelineAnalyser>();
        analyser.AnalyseAsync(Arg.Any<PipelineDocument>(), Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>())
                .Returns(MakeResult([MakeDiagnostic("ADOG-STEPS-001", line: 7)]));
        PipelineAnalysisTools sut = MakeSut(
            parser,
            analyser,
            repository: new GuidelineRepository([MakeGuideline("ADOG-STEPS-001")]));
        string tempDirectory = CreateTempDirectory();
        await File.WriteAllTextAsync(Path.Combine(tempDirectory, "pipeline.yml"), "steps: []");

        try
        {
            // Act
            string result = await sut.AnalyzePipelinePathsAsync([tempDirectory], format: "markdown");

            // Assert
            result.Should().Contain("## Azure Pipelines Guideline Analysis");
            result.Should().Contain("| Severity | Count |");
            result.Should().Contain("[ADOG-STEPS-001](https://learn.microsoft.com/azure/devops/pipelines/process/templates)");
            result.Should().Contain("Use templates");
            result.Should().Contain("| File | Errors | Warnings | Info |");
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task AnalyzePipelinePathsAsync_GivenJsonFormat_ShouldReturnStructuredResponse()
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
            string result = await sut.AnalyzePipelinePathsAsync([tempDirectory], format: "json");

            // Assert
            Deserialize<JsonElement>(result).TryGetProperty("files", out _).Should().BeTrue();
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task AnalyzePipelinePathsAsync_GivenUnknownFormat_ShouldReturnErrorResponse()
    {
        // Arrange
        PipelineAnalysisTools sut = MakeSut();

        // Act
        string result = await sut.AnalyzePipelinePathsAsync(["pipeline.yml"], format: "html");

        // Assert
        JsonElement response = Deserialize<JsonElement>(result);
        response.GetProperty("error").GetString().Should().Contain("Allowed values: json, markdown");
    }

    // ── category filter ─────────────────────────────────────────────────────

    [Fact]
    public async Task AnalyzePipelineAsync_GivenValidCategory_ShouldPassIncludedCategoriesToAnalyser()
    {
        // Arrange
        IPipelineParser parser = Substitute.For<IPipelineParser>();
        parser.Parse(Arg.Any<string>(), Arg.Any<string>()).Returns(EmptyDocument());

        IPipelineAnalyser analyser = Substitute.For<IPipelineAnalyser>();
        analyser.AnalyseAsync(Arg.Any<PipelineDocument>(), Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>())
                .Returns(MakeResult([]));

        PipelineAnalysisTools sut = MakeSut(parser, analyser);

        // Act
        await sut.AnalyzePipelineAsync("steps: []", category: "steps");

        // Assert
        await analyser.Received(1).AnalyseAsync(
            Arg.Any<PipelineDocument>(),
            Arg.Is<AnalysisOptions>(o =>
                o.IncludedCategories != null &&
                o.IncludedCategories.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnalyzePipelineAsync_GivenUnknownCategory_ShouldReturnErrorResponse()
    {
        // Arrange
        PipelineAnalysisTools sut = MakeSut();

        // Act
        string result = await sut.AnalyzePipelineAsync("steps: []", category: "not-a-category");

        // Assert
        JsonElement obj = Deserialize<JsonElement>(result);
        obj.GetProperty("error").GetString().Should().Contain("not-a-category");
    }

    [Fact]
    public async Task AnalyzePipelinePathsAsync_GivenValidCategory_ShouldPassIncludedCategoriesToAnalyser()
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
            await sut.AnalyzePipelinePathsAsync([tempDirectory], category: "jobs");

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
    public async Task AnalyzePipelinePathsAsync_GivenUnknownCategory_ShouldReturnErrorResponse()
    {
        // Arrange
        string tempDirectory = CreateTempDirectory();
        await File.WriteAllTextAsync(Path.Combine(tempDirectory, "pipeline.yml"), "steps: []");

        try
        {
            PipelineAnalysisTools sut = MakeSut(pathResolver: new PipelinePathResolver());

            // Act
            string result = await sut.AnalyzePipelinePathsAsync([tempDirectory], category: "not-a-category");

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
