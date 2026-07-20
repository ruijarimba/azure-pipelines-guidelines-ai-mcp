using System.CommandLine;
using System.CommandLine.Invocation;

namespace AzurePipelines.Guidelines.Cli;

/// <summary>
/// Resolves effective analyze-command options from command-line, environment, and file settings.
/// </summary>
internal static class AnalyzeCommandOptionResolver
{
    internal static AnalyzeCommandOptions ResolveOptions(
        InvocationContext context,
        Argument<string[]> pathArg,
        Option<string> formatOpt,
        Option<string[]?> severityOpt,
        Option<string[]?> categoryOpt,
        Option<string?> outputOpt,
        Option<bool> softFailOpt,
        Option<bool> noColorOpt,
        Option<bool> quietOpt,
        Option<bool> verboseOpt,
        Option<bool> includeHeuristicsOpt,
        AnalyzeCommandEnvironment environment,
        CliConfiguration? configuration = null)
    {
        string[] paths = context.ParseResult.GetValueForArgument(pathArg) ?? [];
        string format = ResolveStringOption(
            context,
            formatOpt,
            "--format",
            environment.Format,
            configuration?.GetFormatValue());
        string[]? severity = ResolveStringArrayOption(
            context,
            severityOpt,
            "--severity",
            environment.Severity,
            configuration?.GetSeverityValue());
        string[]? category = ResolveStringArrayOption(
            context,
            categoryOpt,
            "--category",
            environment.Category,
            configuration?.GetCategoryValue());
        string? output = ResolveOutputOption(
            context,
            outputOpt,
            environment.Output,
            configuration?.GetOutputValue());
        bool softFail = ResolveBooleanOption(
            context,
            softFailOpt,
            "--soft-fail",
            environment.SoftFail,
            configuration?.GetSoftFailValue());
        bool noColor = ResolveBooleanOption(
            context,
            noColorOpt,
            "--no-color",
            environment.NoColor,
            configuration?.GetNoColorValue());
        bool quiet = ResolveQuietOption(
            context,
            quietOpt,
            environment.Quiet,
            configuration?.GetQuietValue());
        bool verbose = ResolveVerboseOption(
            context,
            verboseOpt,
            environment.Verbose,
            configuration?.GetVerboseValue());
        bool includeHeuristics = context.ParseResult.GetValueForOption(includeHeuristicsOpt);

        return new AnalyzeCommandOptions(
            Paths: paths,
            Format: format,
            Severity: severity,
            Category: category,
            Output: output,
            SoftFail: softFail,
            NoColor: noColor,
            Quiet: quiet,
            Verbose: verbose,
            IncludeHeuristics: includeHeuristics);
    }

    internal static string ResolveStringOption(
        InvocationContext context,
        Option<string> option,
        string token,
        string? environmentValue,
        string? configValue)
    {
        if (AnalyzeCommandEnvironment.IsSetByUser(context.ParseResult, token))
        {
            return context.ParseResult.GetValueForOption(option)!;
        }

        if (!string.IsNullOrWhiteSpace(environmentValue))
        {
            return environmentValue;
        }

        if (!string.IsNullOrWhiteSpace(configValue))
        {
            return configValue;
        }

        return context.ParseResult.GetValueForOption(option)!;
    }

    internal static string[]? ResolveStringArrayOption(
        InvocationContext context,
        Option<string[]?> option,
        string token,
        string? environmentValue,
        string? configValue)
    {
        if (AnalyzeCommandEnvironment.IsSetByUser(context.ParseResult, token))
        {
            return NormalizeValues(context.ParseResult.GetValueForOption(option));
        }

        if (!string.IsNullOrWhiteSpace(environmentValue))
        {
            return NormalizeValues(SplitValues(environmentValue));
        }

        if (!string.IsNullOrWhiteSpace(configValue))
        {
            return NormalizeValues(SplitValues(configValue));
        }

        return NormalizeValues(context.ParseResult.GetValueForOption(option));
    }

    private static string[]? NormalizeValues(string[]? values)
    {
        if (values is null || values.Length == 0)
        {
            return null;
        }

        return values.SelectMany(SplitValues).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static string[] SplitValues(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    internal static string? ResolveOutputOption(
        InvocationContext context,
        Option<string?> option,
        string? environmentValue,
        string? configValue)
    {
        if (AnalyzeCommandEnvironment.IsSetByUser(context.ParseResult, "--output") ||
            AnalyzeCommandEnvironment.IsSetByUser(context.ParseResult, "-o"))
        {
            return context.ParseResult.GetValueForOption(option);
        }

        if (!string.IsNullOrWhiteSpace(environmentValue))
        {
            return environmentValue;
        }

        if (!string.IsNullOrWhiteSpace(configValue))
        {
            return configValue;
        }

        return context.ParseResult.GetValueForOption(option);
    }

    internal static bool ResolveBooleanOption(
        InvocationContext context,
        Option<bool> option,
        string token,
        bool? environmentValue,
        bool? configValue)
    {
        if (AnalyzeCommandEnvironment.IsSetByUser(context.ParseResult, token))
        {
            return context.ParseResult.GetValueForOption(option);
        }

        if (environmentValue.HasValue)
        {
            return environmentValue.Value;
        }

        if (configValue.HasValue)
        {
            return configValue.Value;
        }

        return context.ParseResult.GetValueForOption(option);
    }

    internal static bool ResolveQuietOption(
        InvocationContext context,
        Option<bool> option,
        bool? environmentValue,
        bool? configValue)
    {
        if (AnalyzeCommandEnvironment.IsSetByUser(context.ParseResult, "--quiet") ||
            AnalyzeCommandEnvironment.IsSetByUser(context.ParseResult, "-q"))
        {
            return context.ParseResult.GetValueForOption(option);
        }

        if (environmentValue.HasValue)
        {
            return environmentValue.Value;
        }

        if (configValue.HasValue)
        {
            return configValue.Value;
        }

        return context.ParseResult.GetValueForOption(option);
    }

    internal static bool ResolveVerboseOption(
        InvocationContext context,
        Option<bool> option,
        bool? environmentValue,
        bool? configValue)
    {
        if (AnalyzeCommandEnvironment.IsSetByUser(context.ParseResult, "--verbose") ||
            AnalyzeCommandEnvironment.IsSetByUser(context.ParseResult, "-v"))
        {
            return context.ParseResult.GetValueForOption(option);
        }

        if (environmentValue.HasValue)
        {
            return environmentValue.Value;
        }

        if (configValue.HasValue)
        {
            return configValue.Value;
        }

        return context.ParseResult.GetValueForOption(option);
    }
}
