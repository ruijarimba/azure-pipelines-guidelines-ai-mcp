using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using AzurePipelines.Guidelines.Analysis;
using AzurePipelines.Guidelines.Cli;
using AzurePipelines.Guidelines.Core;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AzurePipelines.Guidelines.Cli.Tests;

public sealed class AnalyzeCommandOptionResolverTests
{
    [Fact]
    public void ResolveOptions_GivenCliValues_ShouldUseCliValuesOverEnvironment()
    {
        // Arrange
        using EnvironmentVariableScope scope = new(new Dictionary<string, string?>
        {
            ["ADOG_FORMAT"] = "json",
            ["ADOG_SEVERITY"] = "warning",
            ["ADOG_CATEGORY"] = "jobs",
            ["ADOG_OUTPUT"] = "env-output.txt",
            ["ADOG_SOFT_FAIL"] = "true",
            ["ADOG_NO_COLOR"] = "true",
            ["ADOG_QUIET"] = "true",
            ["ADOG_VERBOSE"] = "true",
        });

        AnalyzeCommandOptions options = ResolveOptions(
            ["fixture.yml", "--format", "compact", "--severity", "error,warning", "--category", "steps,jobs", "--output", "cli-output.txt", "--soft-fail", "--no-color", "--quiet", "--verbose"]);

        // Assert
        options.Format.Should().Be("compact");
        options.Severity.Should().BeEquivalentTo(["error", "warning"]);
        options.Category.Should().BeEquivalentTo(["steps", "jobs"]);
        options.Output.Should().Be("cli-output.txt");
        options.SoftFail.Should().BeTrue();
        options.NoColor.Should().BeTrue();
        options.Quiet.Should().BeTrue();
        options.Verbose.Should().BeTrue();
    }

    [Fact]
    public void ResolveOptions_GivenOnlyEnvironmentValues_ShouldUseEnvironmentValues()
    {
        // Arrange
        using EnvironmentVariableScope scope = new(new Dictionary<string, string?>
        {
            ["ADOG_FORMAT"] = "json",
            ["ADOG_SEVERITY"] = "error,warning",
            ["ADOG_CATEGORY"] = "steps",
            ["ADOG_OUTPUT"] = "env-output.txt",
            ["ADOG_SOFT_FAIL"] = "true",
            ["ADOG_NO_COLOR"] = "true",
            ["ADOG_QUIET"] = "true",
            ["ADOG_VERBOSE"] = "true",
        });

        AnalyzeCommandOptions options = ResolveOptions(["fixture.yml"]);

        // Assert
        options.Format.Should().Be("json");
        options.Severity.Should().BeEquivalentTo(["error", "warning"]);
        options.Category.Should().BeEquivalentTo(["steps"]);
        options.Output.Should().Be("env-output.txt");
        options.SoftFail.Should().BeTrue();
        options.NoColor.Should().BeTrue();
        options.Quiet.Should().BeTrue();
        options.Verbose.Should().BeTrue();
    }

    [Fact]
    public void ResolveOptions_GivenNoCliOrEnvironmentValues_ShouldUseDefaults()
    {
        // Arrange
        using EnvironmentVariableScope scope = new(new Dictionary<string, string?>
        {
            ["ADOG_FORMAT"] = null,
            ["ADOG_SEVERITY"] = null,
            ["ADOG_CATEGORY"] = null,
            ["ADOG_OUTPUT"] = null,
            ["ADOG_SOFT_FAIL"] = null,
            ["ADOG_NO_COLOR"] = null,
            ["ADOG_QUIET"] = null,
            ["ADOG_VERBOSE"] = null,
        });

        using CurrentDirectoryScope tempScope = new(Path.GetTempPath());
        Environment.SetEnvironmentVariable("HOME", null);
        Environment.SetEnvironmentVariable("USERPROFILE", null);

        AnalyzeCommandOptions options = ResolveOptions(["fixture.yml"]);

        // Assert
        options.Format.Should().Be("console");
        options.Severity.Should().BeNull();
        options.Category.Should().BeNull();
        options.Output.Should().BeNull();
        options.SoftFail.Should().BeFalse();
        options.NoColor.Should().BeFalse();
        options.Quiet.Should().BeFalse();
        options.Verbose.Should().BeFalse();
    }

    [Fact]
    public void ResolveOptions_GivenCommaSeparatedAndRepeatedValues_ShouldNormalizeAndDeduplicate()
    {
        // Arrange
        using EnvironmentVariableScope scope = new(new Dictionary<string, string?>
        {
            ["ADOG_SEVERITY"] = null,
            ["ADOG_CATEGORY"] = null,
        });

        AnalyzeCommandOptions options = ResolveOptions(["fixture.yml", "--severity", "warning,error,warning", "--category", "steps,jobs,steps"]);

        // Assert
        options.Severity.Should().BeEquivalentTo(["warning", "error"]);
        options.Category.Should().BeEquivalentTo(["steps", "jobs"]);
    }

    [Fact]
    public void ResolveOptions_GivenConfigFileValues_ShouldUseConfigValuesWhenNoCliOrEnvironmentValues()
    {
        // Arrange
        string configDirectory = Path.Combine(Path.GetTempPath(), $"adog-config-{Guid.NewGuid():N}");
        Directory.CreateDirectory(configDirectory);

        try
        {
            File.WriteAllText(
                Path.Combine(configDirectory, "adog.json"),
                "{\"format\":\"json\",\"severity\":\"warning\",\"category\":\"steps\",\"output\":\"config-output.txt\",\"soft-fail\":\"true\",\"no-color\":\"true\",\"quiet\":\"true\",\"verbose\":\"true\"}");

            using CurrentDirectoryScope scope = new(configDirectory);
            Environment.SetEnvironmentVariable("HOME", configDirectory);
            Environment.SetEnvironmentVariable("USERPROFILE", configDirectory);

            // Act
            AnalyzeCommandOptions options = ResolveOptions(["fixture.yml"]);

            // Assert
            options.Format.Should().Be("json");
            options.Severity.Should().BeEquivalentTo(["warning"]);
            options.Category.Should().BeEquivalentTo(["steps"]);
            options.Output.Should().Be("config-output.txt");
            options.SoftFail.Should().BeTrue();
            options.NoColor.Should().BeTrue();
            options.Quiet.Should().BeTrue();
            options.Verbose.Should().BeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable("HOME", null);
            Environment.SetEnvironmentVariable("USERPROFILE", null);
            if (Directory.Exists(configDirectory))
            {
                Directory.Delete(configDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RunAsync_GivenAnalyzeCommandOptions_ShouldRespectResolvedOptions()
    {
        // Arrange
        string fixturePath = GetFixturePath("missing-timeout-pipeline.yml");
        IPipelineParser parser = CreateParser();
        AnalysisOptions? capturedAnalysisOptions = null;
        IPipelineAnalyser analyser = Substitute.For<IPipelineAnalyser>();

        analyser.AnalyseAsync(
                Arg.Any<PipelineDocument>(),
                Arg.Any<AnalysisOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                PipelineDocument document = callInfo.ArgAt<PipelineDocument>(0);
                capturedAnalysisOptions = callInfo.ArgAt<AnalysisOptions?>(1);
                return Task.FromResult(new AnalysisResult(document, []));
            });

        AnalyzeCommandOptions options = new(
            Paths: [fixturePath],
            Format: "json",
            Severity: ["warning"],
            Category: ["steps"],
            Output: null,
            SoftFail: false,
            NoColor: true,
            Quiet: false,
            Verbose: false);

        // Act
        int exitCode = await AnalyzeCommand.RunAsync(parser, analyser, new PipelinePathResolver(), options);

        // Assert
        exitCode.Should().Be(ExitCodes.Success);
        capturedAnalysisOptions.Should().NotBeNull();
        capturedAnalysisOptions!.MinimumSeverity.Should().Be(DiagnosticSeverity.Warning);
        capturedAnalysisOptions.IncludedCategories.Should().ContainSingle().Which.Should().Be(GuidelineCategory.Steps);
        capturedAnalysisOptions.IncludedDiagnosticSeverities.Should().ContainSingle().Which.Should().Be(DiagnosticSeverity.Warning);
    }

    private static AnalyzeCommandOptions ResolveOptions(params string[] args)
    {
        Argument<string[]> pathArg = new(
            name: "path",
            description: "One or more paths to Azure Pipelines YAML files or directories to analyse.");
        pathArg.Arity = ArgumentArity.OneOrMore;

        Option<string> formatOpt = new(
            name: "--format",
            getDefaultValue: () => "console");

        Option<string[]?> severityOpt = new(
            name: "--severity",
            getDefaultValue: () => null);

        Option<string[]?> categoryOpt = new(
            name: "--category",
            getDefaultValue: () => null);

        Option<string?> outputOpt = new(
            name: "--output",
            getDefaultValue: () => null);

        Option<bool> softFailOpt = new(
            name: "--soft-fail",
            getDefaultValue: () => false);

        Option<bool> noColorOpt = new(
            name: "--no-color",
            getDefaultValue: () => false);

        Option<bool> quietOpt = new(
            name: "--quiet",
            getDefaultValue: () => false);

        Option<bool> verboseOpt = new(
            name: "--verbose",
            getDefaultValue: () => false);

        Command command = new("analyze")
        {
            pathArg,
            formatOpt,
            severityOpt,
            categoryOpt,
            outputOpt,
            softFailOpt,
            noColorOpt,
            quietOpt,
            verboseOpt,
        };

        ParseResult parseResult = command.Parse(args);
        InvocationContext context = new(parseResult);
        AnalyzeCommandEnvironment environment = AnalyzeCommandEnvironment.Load();

        return AnalyzeCommandOptionResolver.ResolveOptions(
            context,
            pathArg,
            formatOpt,
            severityOpt,
            categoryOpt,
            outputOpt,
            softFailOpt,
            noColorOpt,
            quietOpt,
            verboseOpt,
            environment,
            CliConfigurationLoader.Load());
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

    private static string GetFixturePath(string fixtureName)
    {
        string fullPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", fixtureName);
        return Path.GetFullPath(fullPath);
    }

    private sealed class CurrentDirectoryScope : IDisposable
    {
        private readonly string? _originalDirectory = Environment.CurrentDirectory;

        internal CurrentDirectoryScope(string directory)
        {
            Directory.SetCurrentDirectory(directory);
        }

        public void Dispose()
        {
            if (_originalDirectory is not null)
            {
                try
                {
                    Directory.SetCurrentDirectory(_originalDirectory);
                }
                catch (DirectoryNotFoundException)
                {
                    // The original current directory may have been removed by an earlier test.
                    Directory.SetCurrentDirectory(Path.GetTempPath());
                }
            }
        }
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
