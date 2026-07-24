using System.Text;
using AzurePipelines.Guidelines.Core;
using Microsoft.Extensions.Logging;

namespace AzurePipelines.Guidelines.Mcp;

internal static class McpToolInvocationLog
{
    private static readonly Action<ILogger, string, string, Exception?> _toolInvoked =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(3, "McpToolInvoked"),
            "MCP tool invoked: {ToolName}; options: {Options}");

    internal static void Log(
        ILogger logger,
        string toolName,
        string? category = null,
        string? guidelineIds = null,
        AnalysisOptions? effectiveOptions = null)
    {
        if (!logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        StringBuilder options = new();
        AppendOption(options, "category", category);
        AppendOption(options, "guidelineIds", guidelineIds);

        if (effectiveOptions is not null)
        {
            AppendOption(options, "minimumSeverity", effectiveOptions.MinimumSeverity.ToString());
            AppendOption(options, "includedCategories", FormatValues(effectiveOptions.IncludedCategories));
            AppendOption(options, "includedGuidelineIds", FormatValues(effectiveOptions.IncludedGuidelineIds));
            AppendOption(
                options,
                "includedDiagnosticSeverities",
                FormatValues(effectiveOptions.IncludedDiagnosticSeverities));
        }

        _toolInvoked(logger, toolName, options.ToString(), null);
    }

    private static void AppendOption(StringBuilder builder, string name, string? value)
    {
        if (builder.Length > 0)
        {
            builder.Append(", ");
        }

        builder.Append(name).Append('=').Append(value ?? "<default>");
    }

    private static string FormatValues<T>(IReadOnlyList<T>? values)
    {
        if (values is null or { Count: 0 })
        {
            return "<all>";
        }

        return string.Join('|', values);
    }

}
