using System.Globalization;

namespace AzurePipelines.Guidelines.Mcp;

/// <summary>
/// Server defaults for MCP pipeline analysis requests.
/// </summary>
public sealed record McpAnalysisDefaults(
    string? GuidelineIds = null,
    string? Category = null,
    string Format = "json",
    bool IncludeGuidance = false,
    bool IncludeHeuristics = false)
{
    /// <summary>Environment variable for the default guideline ID filter.</summary>
    public const string GuidelineIdsEnvironmentVariable = "ADOG_MCP_GUIDELINE_IDS";
    /// <summary>Environment variable for the default category filter.</summary>
    public const string CategoryEnvironmentVariable = "ADOG_MCP_CATEGORY";
    /// <summary>Environment variable for the default response format.</summary>
    public const string FormatEnvironmentVariable = "ADOG_MCP_FORMAT";
    /// <summary>Environment variable for the default guidance setting.</summary>
    public const string IncludeGuidanceEnvironmentVariable = "ADOG_MCP_INCLUDE_GUIDANCE";
    /// <summary>Environment variable for the default heuristic setting.</summary>
    public const string IncludeHeuristicsEnvironmentVariable = "ADOG_MCP_INCLUDE_HEURISTICS";

    /// <summary>Reads MCP defaults from command-line options and environment variables.</summary>
    /// <remarks>Command-line values take precedence over environment variables.</remarks>
    public static McpAnalysisDefaults FromConfiguration(
        string[]? args = null,
        Func<string, string?>? environment = null)
    {
        args ??= [];
        environment ??= Environment.GetEnvironmentVariable;

        string? guidelineIds = GetStringOption(
            args, "--guideline-ids", environment(GuidelineIdsEnvironmentVariable));
        string? category = GetStringOption(
            args, "--category", environment(CategoryEnvironmentVariable));
        string format = GetStringOption(
            args, "--format", environment(FormatEnvironmentVariable)) ?? "json";
        bool includeGuidance = GetBooleanOption(
            args, "--include-guidance", environment(IncludeGuidanceEnvironmentVariable), false);
        bool includeHeuristics = GetBooleanOption(
            args, "--include-heuristics", environment(IncludeHeuristicsEnvironmentVariable), false);

        ValidateFormat(format);
        return new McpAnalysisDefaults(guidelineIds, category, format, includeGuidance, includeHeuristics);
    }

    private static string? GetStringOption(string[] args, string option, string? fallback)
    {
        for (int index = 0; index < args.Length; index++)
        {
            if (!string.Equals(args[index], option, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (index + 1 >= args.Length || args[index + 1].StartsWith('-'))
            {
                throw new ArgumentException($"Option '{option}' requires a value.");
            }

            return args[index + 1];
        }

        return string.IsNullOrWhiteSpace(fallback) ? null : fallback;
    }

    private static bool GetBooleanOption(string[] args, string option, string? fallback, bool defaultValue)
    {
        string? configured = fallback;
        for (int index = 0; index < args.Length; index++)
        {
            if (!string.Equals(args[index], option, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            configured = index + 1 < args.Length && !args[index + 1].StartsWith('-')
                ? args[index + 1]
                : "true";
            break;
        }

        if (string.IsNullOrWhiteSpace(configured))
        {
            return defaultValue;
        }

        if (bool.TryParse(configured, out bool parsed))
        {
            return parsed;
        }

        if (configured is "1" or "yes" or "on")
        {
            return true;
        }

        if (configured is "0" or "no" or "off")
        {
            return false;
        }

        throw new ArgumentException($"Invalid boolean value '{configured}' for '{option}'. Use true, false, 1, 0, yes, or no.");
    }

    private static void ValidateFormat(string format)
    {
        if (!format.Equals("json", StringComparison.OrdinalIgnoreCase) &&
            !format.Equals("compact", StringComparison.OrdinalIgnoreCase) &&
            !format.Equals("markdown", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Invalid MCP format. Allowed values: json, compact, markdown.");
        }
    }
}
