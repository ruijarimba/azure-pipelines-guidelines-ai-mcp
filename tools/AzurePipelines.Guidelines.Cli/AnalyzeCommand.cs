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
            description: "Output format: console (default) or json.",
            getDefaultValue: () => "console");

        Option<string> severityOpt = new(
            name: "--severity",
            description: "Minimum severity to report: error, warning, or info (default).",
            getDefaultValue: () => "info");

        Option<string?> categoryOpt = new(
            name: "--category",
            description: "Limit analysis to a single category: general, jobs, parameters, pipelines, stages, steps, or variables.",
            getDefaultValue: () => null);

        Command command = new("analyze", "Analyse an Azure Pipelines YAML file against the guidelines.")
        {
            pathArg,
            formatOpt,
            severityOpt,
            categoryOpt,
        };

        command.SetHandler(
            async (string[] paths, string format, string severity, string? category) =>
                await RunAsync(parser, analyser, pathResolver, paths, format, severity, category),
            pathArg, formatOpt, severityOpt, categoryOpt);

        return command;
    }

    internal static Task<int> RunAsync(
        IPipelineParser parser,
        IPipelineAnalyser analyser,
        FileInfo path,
        string format,
        string severity)
        => RunAsync(parser, analyser, new PipelinePathResolver(), [path.FullName], format, severity, category: null);

    internal static async Task<int> RunAsync(
        IPipelineParser parser,
        IPipelineAnalyser analyser,
        PipelinePathResolver pathResolver,
        IEnumerable<string> paths,
        string format,
        string severity,
        string? category = null)
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

        string output = format.Equals("json", StringComparison.OrdinalIgnoreCase)
            ? JsonFormatter.Format(results)
            : ConsoleFormatter.Format(results);

        Console.Write(output);

        return results.Any(result => !result.IsClean) ? ExitCodes.Violations : ExitCodes.Clean;
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
