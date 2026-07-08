using System.CommandLine;
using AzurePipelines.Guidelines.Analysis;
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

        Option<string> severityOpt = new(
            name: "--severity",
            description: "Minimum severity to report: error, warning, or info (default).",
            getDefaultValue: () => "info");

        Option<string?> categoryOpt = new(
            name: "--category",
            description: "Limit analysis to a single category: general, jobs, parameters, pipelines, stages, steps, or variables.",
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

                string[] paths = context.ParseResult.GetValueForArgument(pathArg);
                string format = AnalyzeCommandOptionResolver.ResolveStringOption(
                    context, formatOpt, "--format", environment.Format);
                string severity = AnalyzeCommandOptionResolver.ResolveStringOption(
                    context, severityOpt, "--severity", environment.Severity);
                string? category = AnalyzeCommandOptionResolver.ResolveNullableStringOption(
                    context, categoryOpt, "--category", environment.Category);
                string? output = AnalyzeCommandOptionResolver.ResolveOutputOption(
                    context, outputOpt, environment.Output);
                bool softFail = AnalyzeCommandOptionResolver.ResolveBooleanOption(
                    context, softFailOpt, "--soft-fail", environment.SoftFail);
                bool noColor = AnalyzeCommandOptionResolver.ResolveBooleanOption(
                    context, noColorOpt, "--no-color", environment.NoColor);
                bool quiet = AnalyzeCommandOptionResolver.ResolveQuietOption(
                    context, quietOpt, environment.Quiet);
                bool verbose = AnalyzeCommandOptionResolver.ResolveVerboseOption(
                    context, verboseOpt, environment.Verbose);

                int exitCode = await RunAsync(parser, analyser, pathResolver, paths, format, severity, category,
                                              output, softFail, noColor, quiet, verbose);
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
        => RunAsync(parser, analyser, new PipelinePathResolver(), [path.FullName], format, severity,
                   category: null, output: null, softFail: false, noColor: false, quiet: false, verbose: false);

    internal static async Task<int> RunAsync(
        IPipelineParser parser,
        IPipelineAnalyser analyser,
        PipelinePathResolver pathResolver,
        IEnumerable<string> paths,
        string format,
        string severity,
        string? category = null,
        string? output = null,
        bool softFail = false,
        bool noColor = false,
        bool quiet = false,
        bool verbose = false)
    {
        ArgumentNullException.ThrowIfNull(parser);
        ArgumentNullException.ThrowIfNull(analyser);
        ArgumentNullException.ThrowIfNull(pathResolver);
        ArgumentNullException.ThrowIfNull(paths);

        string[] inputPaths = [.. paths];
        if (inputPaths.Length == 0)
        {
            await Console.Error.WriteLineAsync("error: At least one path is required.").ConfigureAwait(false);
            return ExitCodes.Error;
        }

        DiagnosticSeverity minimumSeverity = ParseSeverity(severity);

        IReadOnlyList<GuidelineCategory>? includedCategories = null;
        if (category is not null)
        {
            if (!TryParseCategory(category, out GuidelineCategory parsedCategory))
            {
                await Console.Error.WriteLineAsync(
                    $"error: Unknown category '{category}'. " +
                    "Allowed values: general, jobs, parameters, pipelines, stages, steps, variables.")
                    .ConfigureAwait(false);
                return ExitCodes.Error;
            }

            includedCategories = [parsedCategory];
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

            AnalysisOptions options = new(
                MinimumSeverity: minimumSeverity,
                IncludedCategories: includedCategories);

            AnalysisResult result = await analyser
                .AnalyseAsync(document, options)
                .ConfigureAwait(false);

            results.Add(result);
        }

        // TODO: Replace with formatter factory once all formatters are implemented
        // For now, keep existing behavior
        string formattedOutput = format.Equals("json", StringComparison.OrdinalIgnoreCase)
            ? JsonFormatter.Format(results)
            : ConsoleFormatter.Format(results);

        // Write to file if --output specified, otherwise stdout
        if (!string.IsNullOrWhiteSpace(output))
        {
            try
            {
                await File.WriteAllTextAsync(output, formattedOutput).ConfigureAwait(false);
            }
            catch (IOException ex)
            {
                await Console.Error.WriteLineAsync($"error: Cannot write to file {output}: {ex.Message}").ConfigureAwait(false);
                return ExitCodes.Error;
            }
        }
        else
        {
            Console.Write(formattedOutput);
        }

        // Soft-fail mode: always exit 0 (audit mode)
        if (softFail)
        {
            return ExitCodes.Success;
        }

        return results.Any(result => !result.IsClean) ? ExitCodes.Violations : ExitCodes.Success;
    }

    private static DiagnosticSeverity ParseSeverity(string value) =>
        value.ToUpperInvariant() switch
        {
            "ERROR"   => DiagnosticSeverity.Error,
            "WARNING" => DiagnosticSeverity.Warning,
            _         => DiagnosticSeverity.Info,
        };

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
