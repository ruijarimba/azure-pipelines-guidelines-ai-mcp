using System.CommandLine;
using System.IO;
using AzurePipelines.Guidelines.Analysis;
using AzurePipelines.Guidelines.Cli;
using AzurePipelines.Guidelines.Core;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AzurePipelines.Guidelines.Cli.Tests;

public sealed class AnalyzeCommandTests
{
    [Fact]
    public async Task Create_GivenCategoryAndSeverityFromEnvironment_ShouldApplyEnvironmentValues()
    {
        // Arrange
        using EnvironmentVariableScope scope = new(new Dictionary<string, string?>
        {
            ["ADOG_CATEGORY"] = "steps",
            ["ADOG_SEVERITY"] = "warning",
        });

        string fixturePath = GetFixturePath("clean-pipeline.yml");
        (IPipelineParser parser, IPipelineAnalyser analyser, AnalysisOptionsCapture capture) =
            CreateAnalyserWithCapturedOptions();

        Command command = AnalyzeCommand.Create(parser, analyser, new PipelinePathResolver());

        // Act
        int exitCode = await command.InvokeAsync([fixturePath]);

        // Assert
        exitCode.Should().Be(ExitCodes.Success);
        capture.Value.Should().NotBeNull();
        capture.Value!.MinimumSeverity.Should().Be(DiagnosticSeverity.Warning);
        capture.Value.IncludedCategories.Should().ContainSingle().Which.Should().Be(GuidelineCategory.Steps);
    }

    [Fact]
    public async Task Create_GivenCliCategoryAndEnvironmentCategory_ShouldPreferCliOption()
    {
        // Arrange
        using EnvironmentVariableScope scope = new(new Dictionary<string, string?>
        {
            ["ADOG_CATEGORY"] = "jobs",
        });

        string fixturePath = GetFixturePath("clean-pipeline.yml");
        (IPipelineParser parser, IPipelineAnalyser analyser, AnalysisOptionsCapture capture) =
            CreateAnalyserWithCapturedOptions();

        Command command = AnalyzeCommand.Create(parser, analyser, new PipelinePathResolver());

        // Act
        int exitCode = await command.InvokeAsync([fixturePath, "--category", "steps"]);

        // Assert
        exitCode.Should().Be(ExitCodes.Success);
        capture.Value.Should().NotBeNull();
        capture.Value!.IncludedCategories.Should().ContainSingle().Which.Should().Be(GuidelineCategory.Steps);
    }

    [Fact]
    public async Task Create_GivenMultipleCategoriesAndSeverities_ShouldApplyAllFilters()
    {
        // Arrange
        string fixturePath = GetFixturePath("clean-pipeline.yml");
        (IPipelineParser parser, IPipelineAnalyser analyser, AnalysisOptionsCapture capture) =
            CreateAnalyserWithCapturedOptions();

        Command command = AnalyzeCommand.Create(parser, analyser, new PipelinePathResolver());

        // Act
        int exitCode = await command.InvokeAsync([
            fixturePath,
            "--category",
            "steps,jobs",
            "--severity",
            "error,warning"
        ]);

        // Assert
        exitCode.Should().Be(ExitCodes.Success);
        capture.Value.Should().NotBeNull();
        capture.Value!.MinimumSeverity.Should().Be(DiagnosticSeverity.Warning);
        capture.Value.IncludedCategories.Should().BeEquivalentTo([GuidelineCategory.Steps, GuidelineCategory.Jobs]);
    }

    [Fact]
    public async Task Create_GivenInvalidBooleanEnvironmentValue_ShouldReturnErrorExitCode()
    {
        // Arrange
        using EnvironmentVariableScope scope = new(new Dictionary<string, string?>
        {
            ["ADOG_SOFT_FAIL"] = "definitely",
        });

        string fixturePath = GetFixturePath("clean-pipeline.yml");
        IPipelineParser parser = CreateParser();
        IPipelineAnalyser analyser = CreateAnalyserWithoutDiagnostics();

        Command command = AnalyzeCommand.Create(parser, analyser, new PipelinePathResolver());

        // Act
        int exitCode = await command.InvokeAsync([fixturePath]);

        // Assert
        exitCode.Should().Be(ExitCodes.Error);
        parser.DidNotReceive().Parse(Arg.Any<string>(), Arg.Any<string>());
        await analyser.DidNotReceive().AnalyseAsync(
            Arg.Any<PipelineDocument>(),
            Arg.Any<AnalysisOptions?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_GivenSoftFailFromEnvironment_ShouldReturnSuccessWhenViolationsExist()
    {
        // Arrange
        using EnvironmentVariableScope scope = new(new Dictionary<string, string?>
        {
            ["ADOG_SOFT_FAIL"] = "true",
        });

        string fixturePath = GetFixturePath("missing-timeout-pipeline.yml");
        IPipelineParser parser = CreateParser();
        IPipelineAnalyser analyser = CreateAnalyserWithSingleDiagnostic();

        Command command = AnalyzeCommand.Create(parser, analyser, new PipelinePathResolver());

        // Act
        int exitCode = await command.InvokeAsync([fixturePath]);

        // Assert
        exitCode.Should().Be(ExitCodes.Success);
    }

    [Fact]
    public async Task RunAsync_GivenSingleFormat_ShouldRenderThatFormatter()
    {
        // Arrange
        IPipelineParser parser = CreateParser();
        IPipelineAnalyser analyser = CreateAnalyserWithSingleDiagnostic();
        using StringWriter output = new();
        TextWriter originalOut = Console.Out;
        Console.SetOut(output);

        try
        {
            // Act
            int exitCode = await AnalyzeCommand.RunAsync(
                parser,
                analyser,
                new PipelinePathResolver(),
                [GetFixturePath("clean-pipeline.yml")],
                "json",
                "info");

            // Assert
            exitCode.Should().Be(ExitCodes.Violations);
            output.ToString().Should().Contain("\"summary\"");
            output.ToString().Should().Contain("\"ruleId\"");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task RunAsync_GivenCommaSeparatedFormats_ShouldRenderEachFormatterInOrder()
    {
        // Arrange
        IPipelineParser parser = CreateParser();
        IPipelineAnalyser analyser = CreateAnalyserWithSingleDiagnostic();
        using StringWriter output = new();
        TextWriter originalOut = Console.Out;
        Console.SetOut(output);

        try
        {
            // Act
            int exitCode = await AnalyzeCommand.RunAsync(
                parser,
                analyser,
                new PipelinePathResolver(),
                [GetFixturePath("clean-pipeline.yml")],
                "json,console",
                "info");

            // Assert
            exitCode.Should().Be(ExitCodes.Violations);
            string renderedOutput = output.ToString();
            renderedOutput.Should().Contain("\"summary\"");
            renderedOutput.Should().Contain("ADOG-STEPS-006");
            renderedOutput.Should().Contain("Summary:");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task Create_GivenSoftFailSeverityAndCategory_ShouldApplyFiltersAndReturnSuccess()
    {
        // Arrange
        string fixturePath = GetFixturePath("missing-timeout-pipeline.yml");
        (IPipelineParser parser, IPipelineAnalyser analyser, AnalysisOptionsCapture capture) =
            CreateAnalyserWithCapturedOptions();

        Command command = AnalyzeCommand.Create(parser, analyser, new PipelinePathResolver());

        // Act
        int exitCode = await command.InvokeAsync([
            fixturePath,
            "--soft-fail",
            "--severity",
            "error",
            "--category",
            "steps"
        ]);

        // Assert
        exitCode.Should().Be(ExitCodes.Success);
        capture.Value.Should().NotBeNull();
        capture.Value!.MinimumSeverity.Should().Be(DiagnosticSeverity.Error);
        capture.Value.IncludedCategories.Should().ContainSingle().Which.Should().Be(GuidelineCategory.Steps);
    }

    [Fact]
    public async Task Create_GivenMultipleFormatsAndOutputPath_ShouldWriteCombinedOutputToFile()
    {
        // Arrange
        string fixturePath = GetFixturePath("missing-timeout-pipeline.yml");
        IPipelineParser parser = CreateParser();
        IPipelineAnalyser analyser = CreateAnalyserWithSingleDiagnostic();
        string outputPath = Path.Combine(Path.GetTempPath(), $"adog-{Guid.NewGuid():N}.txt");

        try
        {
            Command command = AnalyzeCommand.Create(parser, analyser, new PipelinePathResolver());

            // Act
            int exitCode = await command.InvokeAsync([fixturePath, "--format", "json,console", "--output", outputPath]);

            // Assert
            exitCode.Should().Be(ExitCodes.Violations);
            File.Exists(outputPath).Should().BeTrue();
            string writtenOutput = await File.ReadAllTextAsync(outputPath);
            writtenOutput.Should().Contain("\"summary\"");
            writtenOutput.Should().Contain("ADOG-STEPS-006");
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task Create_GivenNoColorAndCompactFormat_ShouldSuppressAnsiCodes()
    {
        // Arrange
        string fixturePath = GetFixturePath("missing-timeout-pipeline.yml");
        IPipelineParser parser = CreateParser();
        IPipelineAnalyser analyser = CreateAnalyserWithSingleDiagnostic();
        using StringWriter output = new();
        TextWriter originalOut = Console.Out;
        Console.SetOut(output);

        try
        {
            Command command = AnalyzeCommand.Create(parser, analyser, new PipelinePathResolver());

            // Act
            int exitCode = await command.InvokeAsync([fixturePath, "--no-color", "--quiet", "--format", "compact"]);

            // Assert
            exitCode.Should().Be(ExitCodes.Violations);
            string renderedOutput = output.ToString();
            renderedOutput.Should().Contain("[ADOG-STEPS-006]");
            renderedOutput.Should().NotContain("\u001b[");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task Create_GivenVerboseSarifAndOutput_ShouldWriteSarifFile()
    {
        // Arrange
        string fixturePath = GetFixturePath("missing-timeout-pipeline.yml");
        IPipelineParser parser = CreateParser();
        IPipelineAnalyser analyser = CreateAnalyserWithSingleDiagnostic();
        string outputPath = Path.Combine(Path.GetTempPath(), $"adog-sarif-{Guid.NewGuid():N}.json");

        try
        {
            Command command = AnalyzeCommand.Create(parser, analyser, new PipelinePathResolver());

            // Act
            int exitCode = await command.InvokeAsync([fixturePath, "--verbose", "--format", "sarif", "--output", outputPath]);

            // Assert
            exitCode.Should().Be(ExitCodes.Violations);
            File.Exists(outputPath).Should().BeTrue();
            string writtenOutput = await File.ReadAllTextAsync(outputPath);
            writtenOutput.Should().Contain("\"version\"");
            writtenOutput.Should().Contain("ADOG-STEPS-006");
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task Create_GivenEnvironmentAndCliOutputPrecedence_ShouldPreferCliOutputPath()
    {
        // Arrange
        using EnvironmentVariableScope scope = new(new Dictionary<string, string?>
        {
            ["ADOG_OUTPUT"] = Path.Combine(Path.GetTempPath(), $"env-output-{Guid.NewGuid():N}.txt"),
            ["ADOG_FORMAT"] = "console",
        });

        string fixturePath = GetFixturePath("missing-timeout-pipeline.yml");
        IPipelineParser parser = CreateParser();
        IPipelineAnalyser analyser = CreateAnalyserWithSingleDiagnostic();
        string cliOutputPath = Path.Combine(Path.GetTempPath(), $"cli-output-{Guid.NewGuid():N}.txt");

        try
        {
            Command command = AnalyzeCommand.Create(parser, analyser, new PipelinePathResolver());

            // Act
            int exitCode = await command.InvokeAsync([fixturePath, "--output", cliOutputPath, "--format", "json"]);

            // Assert
            exitCode.Should().Be(ExitCodes.Violations);
            File.Exists(cliOutputPath).Should().BeTrue();
            File.Exists(Environment.GetEnvironmentVariable("ADOG_OUTPUT")!).Should().BeFalse();
            string writtenOutput = await File.ReadAllTextAsync(cliOutputPath);
            writtenOutput.Should().Contain("\"summary\"");
        }
        finally
        {
            if (File.Exists(cliOutputPath))
            {
                File.Delete(cliOutputPath);
            }

            string? envOutputPath = Environment.GetEnvironmentVariable("ADOG_OUTPUT");
            if (!string.IsNullOrWhiteSpace(envOutputPath) && File.Exists(envOutputPath))
            {
                File.Delete(envOutputPath);
            }
        }
    }

    [Fact]
    public async Task Create_GivenUnknownFormat_ShouldReturnErrorExitCodeAndMessage()
    {
        // Arrange
        string fixturePath = GetFixturePath("clean-pipeline.yml");
        IPipelineParser parser = CreateParser();
        IPipelineAnalyser analyser = CreateAnalyserWithoutDiagnostics();
        using StringWriter errorOutput = new();
        TextWriter originalError = Console.Error;
        Console.SetError(errorOutput);

        try
        {
            Command command = AnalyzeCommand.Create(parser, analyser, new PipelinePathResolver());

            // Act
            int exitCode = await command.InvokeAsync([fixturePath, "--format", "xml"]);

            // Assert
            exitCode.Should().Be(ExitCodes.Violations);
            errorOutput.ToString().Should().Contain("Unknown format 'xml'");
        }
        finally
        {
            Console.SetError(originalError);
        }
    }

    /// <summary>Creates substitutes that capture the options passed to analysis.</summary>
    /// <returns>A parser, analyser, and capture object for the test.</returns>
    private static (IPipelineParser parser, IPipelineAnalyser analyser, AnalysisOptionsCapture capture)
        CreateAnalyserWithCapturedOptions()
    {
        AnalysisOptionsCapture capture = new();
        IPipelineParser parser = CreateParser();
        IPipelineAnalyser analyser = Substitute.For<IPipelineAnalyser>();

        analyser.AnalyseAsync(
                Arg.Any<PipelineDocument>(),
                Arg.Any<AnalysisOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                PipelineDocument document = callInfo.ArgAt<PipelineDocument>(0);
                capture.Value = callInfo.ArgAt<AnalysisOptions?>(1);
                return Task.FromResult(new AnalysisResult(document, []));
            });

        return (parser, analyser, capture);
    }

    /// <summary>Creates a parser substitute that returns an empty pipeline document.</summary>
    /// <returns>The configured parser substitute.</returns>
    private static IPipelineParser CreateParser()
    {
        IPipelineParser parser = Substitute.For<IPipelineParser>();

        parser.Parse(Arg.Any<string>(), Arg.Any<string>())
            .Returns(callInfo =>
            {
                string rawContent = callInfo.ArgAt<string>(0);
                string filePath = callInfo.ArgAt<string>(1);
                return new PipelineDocument(filePath, rawContent, [], [], [], [], []);
            });

        return parser;
    }

    /// <summary>Creates an analyser substitute that returns no diagnostics.</summary>
    /// <returns>The configured analyser substitute.</returns>
    private static IPipelineAnalyser CreateAnalyserWithoutDiagnostics()
    {
        IPipelineAnalyser analyser = Substitute.For<IPipelineAnalyser>();

        analyser.AnalyseAsync(
                Arg.Any<PipelineDocument>(),
                Arg.Any<AnalysisOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                PipelineDocument document = callInfo.ArgAt<PipelineDocument>(0);
                return Task.FromResult(new AnalysisResult(document, []));
            });

        return analyser;
    }

    /// <summary>Creates an analyser substitute that returns one timeout diagnostic.</summary>
    /// <returns>The configured analyser substitute.</returns>
    private static IPipelineAnalyser CreateAnalyserWithSingleDiagnostic()
    {
        IPipelineAnalyser analyser = Substitute.For<IPipelineAnalyser>();

        analyser.AnalyseAsync(
                Arg.Any<PipelineDocument>(),
                Arg.Any<AnalysisOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                PipelineDocument document = callInfo.ArgAt<PipelineDocument>(0);
                Diagnostic diagnostic = new(
                    new GuidelineId("ADOG-STEPS-006"),
                    DiagnosticSeverity.Error,
                    "Missing timeout.",
                    document.FilePath,
                    Line: 1,
                    Column: 1);

                return Task.FromResult(new AnalysisResult(document, [diagnostic]));
            });

        return analyser;
    }

    /// <summary>Returns the absolute path to an analyze-command fixture.</summary>
    /// <param name="fixtureName">The fixture file name.</param>
    /// <returns>The absolute fixture path.</returns>
    private static string GetFixturePath(string fixtureName)
    {
        string fullPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", fixtureName);
        return Path.GetFullPath(fullPath);
    }

    /// <summary>Stores the most recent analysis options passed to a substitute.</summary>
    private sealed class AnalysisOptionsCapture
    {
        internal AnalysisOptions? Value { get; set; }
    }

    /// <summary>Restores environment variables changed by a test.</summary>
    private sealed class EnvironmentVariableScope : IDisposable
    {
        private readonly Dictionary<string, string?> _originalValues = [];

        internal EnvironmentVariableScope(IReadOnlyDictionary<string, string?> values)
        {
            foreach ((string key, string? value) in values)
            {
                _originalValues[key] = Environment.GetEnvironmentVariable(key);
                Environment.SetEnvironmentVariable(key, value);
            }
        }

        public void Dispose()
        {
            foreach ((string key, string? value) in _originalValues)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
