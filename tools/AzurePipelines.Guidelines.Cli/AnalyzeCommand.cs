using System.CommandLine;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Cli;

/// <summary>
/// The <c>adog analyze &lt;path&gt;</c> command.
/// </summary>
internal static class AnalyzeCommand
{
    internal static Command Create(IPipelineParser parser, IPipelineAnalyser analyser)
    {
        Argument<FileInfo> pathArg = new(
            name: "path",
            description: "Path to the Azure Pipelines YAML file to analyse.");

        Option<string> formatOpt = new(
            name: "--format",
            description: "Output format: console (default) or json.",
            getDefaultValue: () => "console");

        Option<string> severityOpt = new(
            name: "--severity",
            description: "Minimum severity to report: error, warning, or info (default).",
            getDefaultValue: () => "info");

        Command command = new("analyze", "Analyse an Azure Pipelines YAML file against the guidelines.")
        {
            pathArg,
            formatOpt,
            severityOpt,
        };

        command.SetHandler(
            async (FileInfo path, string format, string severity) =>
                await RunAsync(parser, analyser, path, format, severity),
            pathArg, formatOpt, severityOpt);

        return command;
    }

    internal static async Task<int> RunAsync(
        IPipelineParser parser,
        IPipelineAnalyser analyser,
        FileInfo path,
        string format,
        string severity)
    {
        if (!path.Exists)
        {
            await Console.Error.WriteLineAsync($"error: File not found: {path.FullName}").ConfigureAwait(false);
            return ExitCodes.Error;
        }

        DiagnosticSeverity minimumSeverity = ParseSeverity(severity);

        string yaml;
        try
        {
            yaml = await File.ReadAllTextAsync(path.FullName).ConfigureAwait(false);
        }
        catch (IOException ex)
        {
            await Console.Error.WriteLineAsync($"error: Cannot read file: {ex.Message}").ConfigureAwait(false);
            return ExitCodes.Error;
        }

        PipelineDocument document;
        try
        {
            document = parser.Parse(yaml, path.FullName);
        }
        catch (PipelineParsingException ex)
        {
            await Console.Error.WriteLineAsync($"error: Failed to parse YAML: {ex.Message}").ConfigureAwait(false);
            return ExitCodes.Error;
        }

        AnalysisOptions options = new(MinimumSeverity: minimumSeverity);
        AnalysisResult result = await analyser
            .AnalyseAsync(document, options)
            .ConfigureAwait(false);

        string output = format.Equals("json", StringComparison.OrdinalIgnoreCase)
            ? JsonFormatter.Format(result)
            : ConsoleFormatter.Format(result);

        Console.Write(output);

        return result.IsClean ? ExitCodes.Clean : ExitCodes.Violations;
    }

    private static DiagnosticSeverity ParseSeverity(string value) =>
        value.ToUpperInvariant() switch
        {
            "ERROR"   => DiagnosticSeverity.Error,
            "WARNING" => DiagnosticSeverity.Warning,
            _         => DiagnosticSeverity.Info,
        };
}
