using System.Text.Json;
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
        IPipelineAnalyser? analyser = null) =>
        new(
            parser ?? Substitute.For<IPipelineParser>(),
            analyser ?? Substitute.For<IPipelineAnalyser>());

    private static T Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json)!;

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
    public async Task AnalyzePipelineAsync_GivenCleanPipeline_ShouldReturnEmptyArray()
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
        JsonElement[] items = Deserialize<JsonElement[]>(result);
        items.Should().BeEmpty();
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
        JsonElement[] items = Deserialize<JsonElement[]>(result);
        items.Should().HaveCount(1);

        JsonElement item = items[0];
        item.GetProperty("ruleId").GetString().Should().Be("ADOG-STEPS-001");
        item.GetProperty("severity").GetString().Should().Be("error");
        item.GetProperty("message").GetString().Should().Be("Use templates.");
        item.GetProperty("line").GetInt32().Should().Be(7);
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
        JsonElement[] items = Deserialize<JsonElement[]>(result);
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
        JsonElement[] items = Deserialize<JsonElement[]>(result);
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
        JsonElement[] items = Deserialize<JsonElement[]>(result);
        items[0].TryGetProperty("line", out _).Should().BeFalse();
    }
}
