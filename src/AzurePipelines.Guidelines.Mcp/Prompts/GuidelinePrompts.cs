using System.ComponentModel;
using ModelContextProtocol.Server;

namespace AzurePipelines.Guidelines.Mcp.Prompts;

/// <summary>
/// Read-only prompt templates for reviewing pipelines and exploring the guideline catalogue.
/// </summary>
[McpServerPromptType]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance", "CA1812:Avoid uninstantiated internal classes",
    Justification = "Instantiated by the MCP SDK via dependency injection.")]
internal sealed class GuidelinePrompts
{
    [McpServerPrompt(Name = "review")]
    [Description("Reviews inline YAML, a file, or a directory using the appropriate analysis tool.")]
    internal static string Review(
        [Description("Inline Azure Pipelines YAML, or a file or directory path.")] string fileOrPath) =>
        $"""
        Perform a read-only Azure Pipelines guideline review for this target:

        {fileOrPath}

        If the target is a file or directory path, call the analyze_pipeline_paths tool.
        Otherwise, treat it as inline YAML and call the analyze_pipeline tool.
        Report diagnostics grouped by file and severity, including rule IDs, locations, and explanations.
        Do not modify files or generate patches.
        """;

    [McpServerPrompt(Name = "review-category")]
    [Description("Reviews inline YAML, a file, or a directory for one guideline category.")]
    internal static string ReviewCategory(
        [Description("Inline Azure Pipelines YAML, or a file or directory path.")] string fileOrPath,
        [Description("Guideline category, such as jobs, steps, stages, or variables.")] string category) =>
        $"""
        Perform a read-only Azure Pipelines guideline review for this target and category:

        Target: {fileOrPath}
        Category: {category}

        If the target is a file or directory path, call analyze_pipeline_paths.
        Otherwise, call analyze_pipeline with inline YAML.
        Use only guideline IDs from the requested category and report the resulting diagnostics.
        Do not modify files or generate patches.
        """;

    [McpServerPrompt(Name = "review-guideline")]
    [Description("Reviews inline YAML, a file, or a directory against selected guideline IDs.")]
    internal static string ReviewGuidelines(
        [Description("Inline Azure Pipelines YAML, or a file or directory path.")] string fileOrPath,
        [Description("Comma-separated guideline IDs, such as ADOG-STEPS-001,ADOG-JOBS-006.")] string guidelineIds) =>
        $"""
        Perform a read-only Azure Pipelines guideline review for this target:

        Target: {fileOrPath}
        Guideline IDs: {guidelineIds}

        If the target is a file or directory path, call analyze_pipeline_paths.
        Otherwise, call analyze_pipeline with inline YAML.
        Restrict the analysis to the supplied guideline IDs and report the diagnostics.
        Do not modify files or generate patches.
        """;

    [McpServerPrompt(Name = "explain-guideline")]
    [Description("Explains one Azure Pipelines guideline from the catalogue.")]
    internal static string ExplainGuideline(
        [Description("The stable guideline ID, such as ADOG-STEPS-001.")] string guidelineId,
        [Description("Optional detail level: summary or full.")] string? detail = null) =>
        $"""
        Give a read-only explanation of this Azure Pipelines guideline:

        Guideline ID: {guidelineId}
        Detail level: {detail ?? "full"}

        Call get_guideline and include the guideline intent, severity, detection guidance,
        automation metadata, and references when available. Do not propose file changes.
        """;

    [McpServerPrompt(Name = "find-guidelines")]
    [Description("Searches the guideline catalogue for rules relevant to a topic.")]
    internal static string FindGuidelines(
        [Description("A word or phrase to search for in guideline titles and descriptions.")] string query,
        [Description("Optional category filter.")] string? category = null) =>
        $"""
        Search the Azure Pipelines guideline catalogue in read-only mode.

        Search text: {query}
        Category: {category ?? "all"}

        Call search_guidelines, applying the category filter when supplied.
        Return the most relevant guideline IDs, titles, severities, and concise summaries.
        """;

    [McpServerPrompt(Name = "list-guidelines")]
    [Description("Lists Azure Pipelines guideline summaries, optionally filtered by category.")]
    internal static string ListGuidelines(
        [Description("Optional category filter.")] string? category = null) =>
        $"""
        List the available Azure Pipelines guidelines in read-only mode.

        Category: {category ?? "all"}

        Call list_guidelines, applying the category filter when supplied.
        Return each guideline ID, title, category, and severity.
        """;

    [McpServerPrompt(Name = "list-categories")]
    [Description("Lists the Azure Pipelines guideline categories.")]
    internal static string ListCategories() =>
        """
        List the available Azure Pipelines guideline categories in read-only mode.
        Call list_categories and briefly describe the scope of each returned category.
        Do not propose file changes.
        """;
}
