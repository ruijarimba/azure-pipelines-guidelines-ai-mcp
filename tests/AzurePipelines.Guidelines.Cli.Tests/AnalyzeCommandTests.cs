using AzurePipelines.Guidelines.Analysis;
using AzurePipelines.Guidelines.Cli;
using AzurePipelines.Guidelines.Core;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AzurePipelines.Guidelines.Cli.Tests;

public sealed class AnalyzeCommandTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static FileInfo FixtureFile(string name) =>
        new(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "adog-cli-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static PipelineDocument EmptyDocument(string filePath = "(test)") =>
        new(
            filePath,
            string.Empty,
            [],
            [],
            [],
            [],
            []);

    private static AnalysisResult CleanResult(string filePath = "(test)") =>
        new(EmptyDocument(filePath), []);

    private static AnalysisResult ViolationResult(string filePath = "(test)") =>
        new(
            EmptyDocument(filePath),
            [new Diagnostic(
                new GuidelineId("ADOG-STEPS-006"),
                DiagnosticSeverity.Error,
                "Task step is missing a timeout.",
                filePath,
                Line: 2,
                Column: null)]);

    // ── File-not-found guard ──────────────────────────────────────────────────

    [Fact]
    public async Task RunAsync_GivenMissingFile_ShouldReturnExitCodeError()
    {
        // Arrange
        IPipelineParser parser = Substitute.For<IPipelineParser>();
        IPipelineAnalyser analyser = Substitute.For<IPipelineAnalyser>();
        FileInfo missing = new(Path.Combine(AppContext.BaseDirectory, "nonexistent.yml"));

        // Act
        int exitCode = await AnalyzeCommand.RunAsync(parser, analyser, missing, "console", "info");

        // Assert
        exitCode.Should().Be(ExitCodes.Error);
    }

    // ── Parsing failure ───────────────────────────────────────────────────────

    [Fact]
    public async Task RunAsync_GivenUnparsableYaml_ShouldReturnExitCodeError()
    {
        // Arrange
        IPipelineParser parser = Substitute.For<IPipelineParser>();
        parser.Parse(Arg.Any<string>(), Arg.Any<string>())
              .Returns(_ => throw new PipelineParsingException("Bad YAML."));

        IPipelineAnalyser analyser = Substitute.For<IPipelineAnalyser>();
        FileInfo file = FixtureFile("clean-pipeline.yml");

        // Act
        int exitCode = await AnalyzeCommand.RunAsync(parser, analyser, file, "console", "info");

        // Assert
        exitCode.Should().Be(ExitCodes.Error);
    }

    // ── Clean pipeline ────────────────────────────────────────────────────────

    [Fact]
    public async Task RunAsync_GivenCleanPipeline_ShouldReturnExitCodeClean()
    {
        // Arrange
        IPipelineParser parser = Substitute.For<IPipelineParser>();
        parser.Parse(Arg.Any<string>(), Arg.Any<string>()).Returns(EmptyDocument());

        IPipelineAnalyser analyser = Substitute.For<IPipelineAnalyser>();
        analyser.AnalyseAsync(Arg.Any<PipelineDocument>(), Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>())
                .Returns(CleanResult());

        FileInfo file = FixtureFile("clean-pipeline.yml");

        // Act
        int exitCode = await AnalyzeCommand.RunAsync(parser, analyser, file, "console", "info");

        // Assert
        exitCode.Should().Be(ExitCodes.Clean);
    }

    // ── Pipeline with violations ──────────────────────────────────────────────

    [Fact]
    public async Task RunAsync_GivenViolations_ShouldReturnExitCodeViolations()
    {
        // Arrange
        IPipelineParser parser = Substitute.For<IPipelineParser>();
        parser.Parse(Arg.Any<string>(), Arg.Any<string>()).Returns(EmptyDocument());

        IPipelineAnalyser analyser = Substitute.For<IPipelineAnalyser>();
        analyser.AnalyseAsync(Arg.Any<PipelineDocument>(), Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>())
                .Returns(ViolationResult());

        FileInfo file = FixtureFile("missing-timeout-pipeline.yml");

        // Act
        int exitCode = await AnalyzeCommand.RunAsync(parser, analyser, file, "console", "info");

        // Assert
        exitCode.Should().Be(ExitCodes.Violations);
    }

    // ── Severity filter ───────────────────────────────────────────────────────

    [Fact]
    public async Task RunAsync_GivenSeverityFilter_ShouldPassMinimumSeverityToAnalyser()
    {
        // Arrange
        IPipelineParser parser = Substitute.For<IPipelineParser>();
        parser.Parse(Arg.Any<string>(), Arg.Any<string>()).Returns(EmptyDocument());

        IPipelineAnalyser analyser = Substitute.For<IPipelineAnalyser>();
        analyser.AnalyseAsync(Arg.Any<PipelineDocument>(), Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>())
                .Returns(CleanResult());

        FileInfo file = FixtureFile("clean-pipeline.yml");

        // Act
        await AnalyzeCommand.RunAsync(parser, analyser, file, "console", "error");

        // Assert
        await analyser.Received(1).AnalyseAsync(
            Arg.Any<PipelineDocument>(),
            Arg.Is<AnalysisOptions>(o => o.MinimumSeverity == DiagnosticSeverity.Error),
            Arg.Any<CancellationToken>());
    }

    // ── JSON format ───────────────────────────────────────────────────────────

    [Fact]
    public async Task RunAsync_GivenJsonFormat_ShouldNotThrow()
    {
        // Arrange
        IPipelineParser parser = Substitute.For<IPipelineParser>();
        parser.Parse(Arg.Any<string>(), Arg.Any<string>()).Returns(EmptyDocument());

        IPipelineAnalyser analyser = Substitute.For<IPipelineAnalyser>();
        analyser.AnalyseAsync(Arg.Any<PipelineDocument>(), Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>())
                .Returns(ViolationResult());

        FileInfo file = FixtureFile("missing-timeout-pipeline.yml");

        // Act
        Func<Task> act = async () =>
            await AnalyzeCommand.RunAsync(parser, analyser, file, "json", "info");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RunAsync_GivenMultiplePaths_ShouldAnalyseEachDiscoveredFile()
    {
        // Arrange
        IPipelineParser parser = Substitute.For<IPipelineParser>();
        parser.Parse(Arg.Any<string>(), Arg.Any<string>()).Returns(callInfo =>
            EmptyDocument(callInfo.ArgAt<string>(1)));

        IPipelineAnalyser analyser = Substitute.For<IPipelineAnalyser>();
        analyser.AnalyseAsync(Arg.Any<PipelineDocument>(), Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    PipelineDocument document = (PipelineDocument)callInfo[0]!;
                    return document.FilePath.EndsWith("two.yml", StringComparison.OrdinalIgnoreCase)
                        ? ViolationResult(document.FilePath)
                        : CleanResult(document.FilePath);
                });

        string tempDirectory = CreateTempDirectory();
        string firstPath = Path.Combine(tempDirectory, "one.yml");
        string secondPath = Path.Combine(tempDirectory, "two.yml");
        await File.WriteAllTextAsync(firstPath, "steps: []");
        await File.WriteAllTextAsync(secondPath, "steps: []");

        try
        {
            // Act
            int exitCode = await AnalyzeCommand.RunAsync(
                parser,
                analyser,
                new PipelinePathResolver(),
                [firstPath, secondPath],
                "console",
                "info");

            // Assert
            exitCode.Should().Be(ExitCodes.Violations);
            await analyser.Received(2).AnalyseAsync(
                Arg.Any<PipelineDocument>(),
                Arg.Any<AnalysisOptions>(),
                Arg.Any<CancellationToken>());
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task RunAsync_GivenDirectoryInput_ShouldAnalyseFilesRecursively()
    {
        // Arrange
        IPipelineParser parser = Substitute.For<IPipelineParser>();
        parser.Parse(Arg.Any<string>(), Arg.Any<string>()).Returns(callInfo =>
            EmptyDocument(callInfo.ArgAt<string>(1)));

        IPipelineAnalyser analyser = Substitute.For<IPipelineAnalyser>();
        analyser.AnalyseAsync(Arg.Any<PipelineDocument>(), Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>())
                .Returns(callInfo => CleanResult(callInfo.Arg<PipelineDocument>(0).FilePath));

        string tempDirectory = CreateTempDirectory();
        string nestedDirectory = Path.Combine(tempDirectory, "nested");
        Directory.CreateDirectory(nestedDirectory);
        string nestedPath = Path.Combine(nestedDirectory, "pipeline.yaml");
        await File.WriteAllTextAsync(nestedPath, "steps: []");

        try
        {
            // Act
            int exitCode = await AnalyzeCommand.RunAsync(
                parser,
                analyser,
                new PipelinePathResolver(),
                [tempDirectory],
                "console",
                "info");

            // Assert
            exitCode.Should().Be(ExitCodes.Clean);
            await analyser.Received(1).AnalyseAsync(
                Arg.Any<PipelineDocument>(),
                Arg.Any<AnalysisOptions>(),
                Arg.Any<CancellationToken>());
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }
}
