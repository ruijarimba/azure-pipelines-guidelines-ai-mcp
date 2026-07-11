using System.CommandLine;
using System.CommandLine.Invocation;

namespace AzurePipelines.Guidelines.Cli;

internal static class AnalyzeCommandOptionResolver
{
    internal static string ResolveStringOption(
        InvocationContext context,
        Option<string> option,
        string token,
        string? environmentValue)
    {
        if (AnalyzeCommandEnvironment.IsSetByUser(context.ParseResult, token))
        {
            return context.ParseResult.GetValueForOption(option)!;
        }

        return environmentValue ?? context.ParseResult.GetValueForOption(option)!;
    }

    internal static string[]? ResolveStringArrayOption(
        InvocationContext context,
        Option<string[]?> option,
        string token,
        string? environmentValue)
    {
        if (AnalyzeCommandEnvironment.IsSetByUser(context.ParseResult, token))
        {
            return context.ParseResult.GetValueForOption(option);
        }

        if (!string.IsNullOrWhiteSpace(environmentValue))
        {
            return SplitValues(environmentValue);
        }

        string[]? values = context.ParseResult.GetValueForOption(option);
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
        string? environmentValue)
    {
        if (AnalyzeCommandEnvironment.IsSetByUser(context.ParseResult, "--output") ||
            AnalyzeCommandEnvironment.IsSetByUser(context.ParseResult, "-o"))
        {
            return context.ParseResult.GetValueForOption(option);
        }

        return environmentValue ?? context.ParseResult.GetValueForOption(option);
    }

    internal static bool ResolveBooleanOption(
        InvocationContext context,
        Option<bool> option,
        string token,
        bool? environmentValue)
    {
        if (AnalyzeCommandEnvironment.IsSetByUser(context.ParseResult, token))
        {
            return context.ParseResult.GetValueForOption(option);
        }

        return environmentValue ?? context.ParseResult.GetValueForOption(option);
    }

    internal static bool ResolveQuietOption(
        InvocationContext context,
        Option<bool> option,
        bool? environmentValue)
    {
        if (AnalyzeCommandEnvironment.IsSetByUser(context.ParseResult, "--quiet") ||
            AnalyzeCommandEnvironment.IsSetByUser(context.ParseResult, "-q"))
        {
            return context.ParseResult.GetValueForOption(option);
        }

        return environmentValue ?? context.ParseResult.GetValueForOption(option);
    }

    internal static bool ResolveVerboseOption(
        InvocationContext context,
        Option<bool> option,
        bool? environmentValue)
    {
        if (AnalyzeCommandEnvironment.IsSetByUser(context.ParseResult, "--verbose") ||
            AnalyzeCommandEnvironment.IsSetByUser(context.ParseResult, "-v"))
        {
            return context.ParseResult.GetValueForOption(option);
        }

        return environmentValue ?? context.ParseResult.GetValueForOption(option);
    }
}
