using System.CommandLine;
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

    private static string GetFixturePath(string fixtureName)
    {
        string fullPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", fixtureName);
        return Path.GetFullPath(fullPath);
    }

    private sealed class AnalysisOptionsCapture
    {
        internal AnalysisOptions? Value { get; set; }
    }

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
