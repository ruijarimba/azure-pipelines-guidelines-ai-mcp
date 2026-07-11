using System.CommandLine;
using AzurePipelines.Guidelines.Analysis;
using AzurePipelines.Guidelines.Cli.Formatters;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Cli;

/// <summary>
/// The <c>adog analyze &lt;path&gt;</c> command.
/// </summary>
internal static class AnalyzeCommand
{
    internal static Command Create(
        IPipelineParser parser,
        IPipelineAnalyser analyser,
        PipelinePathResolver pathResolver)
    {
        Argument<string[]> pathArg = new(
            name: "path",
            description: "One or more paths to Azure Pipelines YAML files or directories to analyse.");
        pathArg.Arity = ArgumentArity.OneOrMore;

        Option<string> formatOpt = new(
            name: "--format",
            description: "Output format (comma-separated for multiple): console, compact, json, junit, sarif, markdown. Default: console.",
            getDefaultValue: () => "console");

        Option<string[]?> severityOpt = new(
            name: "--severity",
            description: "Minimum severity to report (comma-separated or repeated): error, warning, or info (default).",
            getDefaultValue: () => null);

        Option<string[]?> categoryOpt = new(
            name: "--category",
            description: "Limit analysis to one or more categories: general, jobs, parameters, pipelines, stages, steps, or variables.",
            getDefaultValue: () => null);

        Option<string?> outputOpt = new(
            aliases: ["--output", "-o"],
            description: "Write output to file instead of stdout.");

        Option<bool> softFailOpt = new(
            name: "--soft-fail",
            description: "Always exit with code 0, even if violations are found (audit mode).",
            getDefaultValue: () => false);

        Option<bool> noColorOpt = new(
            name: "--no-color",
            description: "Disable ANSI color codes in console output.",
            getDefaultValue: () => false);

        Option<bool> quietOpt = new(
            aliases: ["--quiet", "-q"],
            description: "Suppress detailed output, show summary only.",
            getDefaultValue: () => false);

        Option<bool> verboseOpt = new(
            aliases: ["--verbose", "-v"],
            description: "Enable detailed logging.",
            getDefaultValue: () => false);

        Command command = new("analyze", "Analyse an Azure Pipelines YAML file against the guidelines.")
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

        command.SetHandler(
            async (context) =>
            {
                AnalyzeCommandEnvironment environment = AnalyzeCommandEnvironment.Load();
                if (!string.IsNullOrWhiteSpace(environment.ErrorMessage))
                {
                    await Console.Error.WriteLineAsync(environment.ErrorMessage).ConfigureAwait(false);
                    context.ExitCode = ExitCodes.Error;
                    return;
                }

                AnalyzeCommandOptions options = AnalyzeCommandOptionResolver.ResolveOptions(
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
                    environment);

                int exitCode = await RunAsync(parser, analyser, pathResolver, options);
                context.ExitCode = exitCode;
            });

        return command;
    }

    internal static Task<int> RunAsync(
        IPipelineParser parser,
        IPipelineAnalyser analyser,
        FileInfo path,
        string format,
        string severity)
        => RunAsync(
            parser,
            analyser,
            new PipelinePathResolver(),
            [path.FullName],
            format,
            severity);

    internal static Task<int> RunAsync(
        IPipelineParser parser,
        IPipelineAnalyser analyser,
        PipelinePathResolver pathResolver,
        IEnumerable<string> paths,
        string format,
        string severity,
        string[]? category = null,
        string? output = null,
        bool softFail = false,
        bool noColor = false,
        bool quiet = false,
        bool verbose = false)
        => RunAsync(
            parser,
            analyser,
            pathResolver,
            new AnalyzeCommandOptions(
                Paths: [.. paths],
                Format: format,
                Severity: [severity],
                Category: category,
                Output: output,
                SoftFail: softFail,
                NoColor: noColor,
                Quiet: quiet,
                Verbose: verbose));

    internal static async Task<int> RunAsync(
        IPipelineParser parser,
        IPipelineAnalyser analyser,
        PipelinePathResolver pathResolver,
        AnalyzeCommandOptions options)
    {
        ArgumentNullException.ThrowIfNull(parser);
        ArgumentNullException.ThrowIfNull(analyser);
        ArgumentNullException.ThrowIfNull(pathResolver);
        ArgumentNullException.ThrowIfNull(options);

        string[] inputPaths = options.Paths;
        if (inputPaths.Length == 0)
        {
            await Console.Error.WriteLineAsync("error: At least one path is required.").ConfigureAwait(false);
            return ExitCodes.Error;
        }

        IReadOnlyList<DiagnosticSeverity>? includedDiagnosticSeverities = null;
        DiagnosticSeverity minimumSeverity = DiagnosticSeverity.Info;
        if (options.Severity is { Length: > 0 })
        {
            List<DiagnosticSeverity> parsedSeverities = [];
            foreach (string severityValue in options.Severity)
            {
                foreach (string part in SplitValues(severityValue))
                {
                    if (!TryParseSeverity(part, out DiagnosticSeverity parsedSeverity))
                    {
                        await Console.Error.WriteLineAsync(
                            $"error: Unknown severity '{part}'. " +
                            "Allowed values: error, warning, info.")
                            .ConfigureAwait(false);
                        return ExitCodes.Error;
                    }

                    parsedSeverities.Add(parsedSeverity);
                }
            }

            if (parsedSeverities.Count > 0)
            {
                includedDiagnosticSeverities = parsedSeverities.Distinct().ToArray();
                minimumSeverity = includedDiagnosticSeverities.Min();
            }
        }

        IReadOnlyList<GuidelineCategory>? includedCategories = null;
        if (options.Category is { Length: > 0 })
        {
            List<GuidelineCategory> parsedCategories = [];
            foreach (string categoryValue in options.Category)
            {
                foreach (string part in SplitValues(categoryValue))
                {
                    if (!TryParseCategory(part, out GuidelineCategory parsedCategory))
                    {
                        await Console.Error.WriteLineAsync(
                            $"error: Unknown category '{part}'. " +
                            "Allowed values: general, jobs, parameters, pipelines, stages, steps, variables.")
                            .ConfigureAwait(false);
                        return ExitCodes.Error;
                    }

                    parsedCategories.Add(parsedCategory);
                }
            }

            includedCategories = parsedCategories.Distinct().ToArray();
        }

        IReadOnlyList<string> discoveredPaths;
        try
        {
            discoveredPaths = pathResolver.Resolve(inputPaths);
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or ArgumentException)
        {
            await Console.Error.WriteLineAsync($"error: {ex.Message}").ConfigureAwait(false);
            return ExitCodes.Error;
        }

        List<AnalysisResult> results = [];
        foreach (string discoveredPath in discoveredPaths)
        {
            string yaml;
            try
            {
                yaml = await File.ReadAllTextAsync(discoveredPath).ConfigureAwait(false);
            }
            catch (IOException ex)
            {
                await Console.Error.WriteLineAsync($"error: Cannot read file {discoveredPath}: {ex.Message}").ConfigureAwait(false);
                return ExitCodes.Error;
            }

            PipelineDocument document;
            try
            {
                document = parser.Parse(yaml, discoveredPath);
            }
            catch (PipelineParsingException ex)
            {
                await Console.Error.WriteLineAsync($"error: Failed to parse YAML in {discoveredPath}: {ex.Message}").ConfigureAwait(false);
                return ExitCodes.Error;
            }

            AnalysisOptions analysisOptions = new(
                MinimumSeverity: minimumSeverity,
                IncludedCategories: includedCategories,
                IncludedDiagnosticSeverities: includedDiagnosticSeverities);

            AnalysisResult result = await analyser
                .AnalyseAsync(document, analysisOptions)
                .ConfigureAwait(false);

            results.Add(result);
        }

        IReadOnlyList<string> requestedFormats;
        try
        {
            requestedFormats = ParseFormats(options.Format);
        }
        catch (ArgumentException ex)
        {
            await Console.Error.WriteLineAsync($"error: {ex.Message}").ConfigureAwait(false);
            return ExitCodes.Error;
        }

        string formattedOutput = FormatResults(results, requestedFormats, useColor: !options.NoColor);

        // Write to file if --output specified, otherwise stdout
        if (!string.IsNullOrWhiteSpace(options.Output))
        {
            try
            {
                await File.WriteAllTextAsync(options.Output, formattedOutput).ConfigureAwait(false);
            }
            catch (IOException ex)
            {
                await Console.Error.WriteLineAsync($"error: Cannot write to file {options.Output}: {ex.Message}").ConfigureAwait(false);
                return ExitCodes.Error;
            }
        }
        else
        {
            Console.Write(formattedOutput);
        }

        // Soft-fail mode: always exit 0 (audit mode)
        if (options.SoftFail)
        {
            return ExitCodes.Success;
        }

        return results.Any(result => !result.IsClean) ? ExitCodes.Violations : ExitCodes.Success;
    }

    private static string FormatResults(
        IReadOnlyList<AnalysisResult> results,
        IReadOnlyList<string> formats,
        bool useColor)
    {
        List<string> renderedSections = [];
        foreach (string requestedFormat in formats)
        {
            IOutputFormatter formatter = OutputFormatterFactory.Get(requestedFormat);
            renderedSections.Add(formatter.Format(results, useColor));
        }

        return string.Join(Environment.NewLine + Environment.NewLine, renderedSections);
    }

    private static string[] ParseFormats(string format)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return ["console"];
        }

        string[] parsedFormats = format.Split(
            ',',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        return parsedFormats.Length == 0 ? ["console"] : parsedFormats;
    }

    private static bool TryParseSeverity(string value, out DiagnosticSeverity result)
    {
        result = value.ToUpperInvariant() switch
        {
            "ERROR"   => DiagnosticSeverity.Error,
            "WARNING" => DiagnosticSeverity.Warning,
            "INFO"    => DiagnosticSeverity.Info,
            _         => (DiagnosticSeverity)(-1),
        };

        return (int)result >= 0;
    }

    private static string[] SplitValues(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    private static bool TryParseCategory(string value, out GuidelineCategory result)
    {
        result = value.ToUpperInvariant() switch
        {
            "GENERAL"    => GuidelineCategory.General,
            "JOBS"       => GuidelineCategory.Jobs,
            "PARAMETERS" => GuidelineCategory.Parameters,
            "PIPELINES"  => GuidelineCategory.Pipelines,
            "STAGES"     => GuidelineCategory.Stages,
            "STEPS"      => GuidelineCategory.Steps,
            "VARIABLES"  => GuidelineCategory.Variables,
            _            => (GuidelineCategory)(-1),
        };

        return (int)result >= 0;
    }
}
