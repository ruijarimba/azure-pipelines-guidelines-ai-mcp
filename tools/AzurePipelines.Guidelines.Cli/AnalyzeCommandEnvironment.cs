using System.CommandLine.Parsing;

namespace AzurePipelines.Guidelines.Cli;

/// <summary>
/// Represents analyze-command settings loaded from environment variables.
/// </summary>
internal sealed class AnalyzeCommandEnvironment
{
    private const string _formatVariableName = "ADOG_FORMAT";
    private const string _severityVariableName = "ADOG_SEVERITY";
    private const string _categoryVariableName = "ADOG_CATEGORY";
    private const string _outputVariableName = "ADOG_OUTPUT";
    private const string _softFailVariableName = "ADOG_SOFT_FAIL";
    private const string _noColorVariableName = "ADOG_NO_COLOR";
    private const string _quietVariableName = "ADOG_QUIET";
    private const string _verboseVariableName = "ADOG_VERBOSE";

    private AnalyzeCommandEnvironment()
    {
    }

    internal string? Format { get; private init; }

    internal string? Severity { get; private init; }

    internal string? Category { get; private init; }

    internal string? Output { get; private init; }

    internal bool? SoftFail { get; private init; }

    internal bool? NoColor { get; private init; }

    internal bool? Quiet { get; private init; }

    internal bool? Verbose { get; private init; }

    internal string? ErrorMessage { get; private init; }

    internal static AnalyzeCommandEnvironment Load()
    {
        string? softFailValue = Environment.GetEnvironmentVariable(_softFailVariableName);
        string? noColorValue = Environment.GetEnvironmentVariable(_noColorVariableName);
        string? quietValue = Environment.GetEnvironmentVariable(_quietVariableName);
        string? verboseValue = Environment.GetEnvironmentVariable(_verboseVariableName);

        if (!TryParseBoolean(softFailValue, _softFailVariableName, out bool? softFail, out string? softFailError))
        {
            return new AnalyzeCommandEnvironment { ErrorMessage = softFailError };
        }

        if (!TryParseBoolean(noColorValue, _noColorVariableName, out bool? noColor, out string? noColorError))
        {
            return new AnalyzeCommandEnvironment { ErrorMessage = noColorError };
        }

        if (!TryParseBoolean(quietValue, _quietVariableName, out bool? quiet, out string? quietError))
        {
            return new AnalyzeCommandEnvironment { ErrorMessage = quietError };
        }

        if (!TryParseBoolean(verboseValue, _verboseVariableName, out bool? verbose, out string? verboseError))
        {
            return new AnalyzeCommandEnvironment { ErrorMessage = verboseError };
        }

        return new AnalyzeCommandEnvironment
        {
            Format = Environment.GetEnvironmentVariable(_formatVariableName),
            Severity = Environment.GetEnvironmentVariable(_severityVariableName),
            Category = Environment.GetEnvironmentVariable(_categoryVariableName),
            Output = Environment.GetEnvironmentVariable(_outputVariableName),
            SoftFail = softFail,
            NoColor = noColor,
            Quiet = quiet,
            Verbose = verbose,
        };
    }

    internal static bool IsSetByUser(ParseResult parseResult, string symbolName)
    {
        ArgumentNullException.ThrowIfNull(parseResult);

        return parseResult.Tokens.Any(token =>
            token.Value.Equals(symbolName, StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryParseBoolean(
        string? value,
        string variableName,
        out bool? parsedValue,
        out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            parsedValue = null;
            errorMessage = null;
            return true;
        }

        switch (value.Trim().ToUpperInvariant())
        {
            case "TRUE":
            case "1":
            case "YES":
                parsedValue = true;
                errorMessage = null;
                return true;

            case "FALSE":
            case "0":
            case "NO":
                parsedValue = false;
                errorMessage = null;
                return true;

            default:
                parsedValue = null;
                errorMessage =
                    $"error: Invalid boolean value '{value}' for environment variable {variableName}. " +
                    "Allowed values: true/false, 1/0, yes/no.";
                return false;
        }
    }
}
