using System.ComponentModel;
using ModelContextProtocol.Server;

namespace AzurePipelines.Guidelines.Mcp.Prompts;

/// <summary>
/// Read-only prompt templates for reviewing pipelines and templates and exploring the guideline catalogue.
/// </summary>
[McpServerPromptType]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance", "CA1812:Avoid uninstantiated internal classes",
    Justification = "Instantiated by the MCP SDK via dependency injection.")]
internal sealed class GuidelinePrompts
{
    [McpServerPrompt(Name = "review", Title = "Review pipeline or template")]
    [Description("Reviews inline YAML, a file, or a directory with the template analysis tool.")]
    internal static string Review(
        [Description("Optional inline Azure Pipelines YAML, or a file or directory path. If omitted, ask the analysis tool to resolve common repository pipeline paths.")]
        string? fileOrPath = null) =>
        $"""
        Perform a read-only Azure Pipelines pipeline or template guideline review for this target:

        {fileOrPath ?? "(automatic repository path resolution)"}

        Call analyze_template_or_folder with exactly one of yaml or fileOrPath. Use yaml for inline content
        and fileOrPath for a file or directory path. If no target was supplied, use a common repository
        path such as pipelines; the tool will try additional common paths when necessary.
        Report findings grouped by file and recommendation label, including rule IDs, locations, and explanations.
        Present recommendations using DO, DO-NOT, AVOID, and CONSIDER only.
        Do not modify files or generate patches.
        """;

    [McpServerPrompt(Name = "review-category", Title = "Review one guideline category")]
    [Description("Reviews inline YAML, a file, or a directory for one guideline category.")]
    internal static string ReviewCategory(
        [Description("Guideline category, such as jobs, steps, stages, or variables.")] string category,
        [Description("Optional inline Azure Pipelines YAML, or a file or directory path. If omitted, ask the analysis tool to resolve common repository pipeline paths.")]
        string? fileOrPath = null) =>
        $"""
        Perform a read-only Azure Pipelines pipeline or template guideline review for this target and category:

        Target: {fileOrPath ?? "(automatic repository path resolution)"}
        Category: {category}

        Call analyze_template_or_folder with exactly one of yaml or fileOrPath. If the target is omitted,
        use a common repository path such as pipelines and allow the tool to try additional paths.
        Use only guideline IDs from the requested category and report the resulting recommendations.
        Present recommendations using DO, DO-NOT, AVOID, and CONSIDER only.
        Do not modify files or generate patches.
        """;

    [McpServerPrompt(Name = "review-guideline", Title = "Review selected guidelines")]
    [Description("Reviews inline YAML, a file, or a directory against selected guideline IDs.")]
    internal static string ReviewGuidelines(
        [Description("Comma-separated guideline IDs, such as ADOG-STEPS-001,ADOG-JOBS-006.")] string guidelineIds,
        [Description("Optional inline Azure Pipelines YAML, or a file or directory path. If omitted, ask the analysis tool to resolve common repository pipeline paths.")]
        string? fileOrPath = null) =>
        $"""
        Perform a read-only Azure Pipelines pipeline or template guideline review for this target:

        Target: {fileOrPath ?? "(automatic repository path resolution)"}
        Guideline IDs: {guidelineIds}

        Call analyze_template_or_folder with exactly one of yaml or fileOrPath. If the target is omitted,
        use a common repository path such as pipelines and allow the tool to try additional paths.
        Restrict the analysis to the supplied guideline IDs and report the recommendations.
        Present recommendations using DO, DO-NOT, AVOID, and CONSIDER only.
        Do not modify files or generate patches.
        """;

    [McpServerPrompt(Name = "explain-guideline", Title = "Explain a guideline")]
    [Description("Explains one Azure Pipelines guideline from the catalogue.")]
    internal static string ExplainGuideline(
        [Description("The stable guideline ID, such as ADOG-STEPS-001.")] string guidelineId,
        [Description("Optional detail level: summary or full.")] string? detail = null) =>
        $"""
        Give a read-only explanation of this Azure Pipelines guideline:

        Guideline ID: {guidelineId}
        Detail level: {detail ?? "full"}

        Call get_guideline and include the guideline intent, recommendation label, detection guidance,
        automation metadata, and references when available. Present the recommendation label using
        DO, DO-NOT, AVOID, and CONSIDER only. Do not propose file changes.
        """;

    [McpServerPrompt(Name = "find-guidelines", Title = "Find guidelines")]
    [Description("Searches the guideline catalogue for rules relevant to a topic.")]
    internal static string FindGuidelines(
        [Description("A word or phrase to search for in guideline titles and descriptions.")] string query,
        [Description("Optional category filter.")] string? category = null) =>
        $"""
        Search the Azure Pipelines guideline catalogue in read-only mode.

        Search text: {query}
        Category: {category ?? "all"}

        Call search_guidelines, applying the category filter when supplied.
        Return the most relevant guideline IDs, titles, recommendation labels, and concise summaries.
        Present recommendation labels using DO, DO-NOT, AVOID, and CONSIDER only.
        """;

    [McpServerPrompt(Name = "list-guidelines", Title = "List guidelines")]
    [Description("Lists Azure Pipelines guideline summaries, optionally filtered by category.")]
    internal static string ListGuidelines(
        [Description("Optional category filter.")] string? category = null) =>
        $"""
        List the available Azure Pipelines guidelines in read-only mode.

        Category: {category ?? "all"}

        Call list_guidelines, applying the category filter when supplied.
        Return each guideline ID, title, category, and recommendation label.
        Present recommendation labels using DO, DO-NOT, AVOID, and CONSIDER only.
        """;

    [McpServerPrompt(Name = "list-categories", Title = "List guideline categories")]
    [Description("Lists the Azure Pipelines guideline categories.")]
    internal static string ListCategories() =>
        """
        List the available Azure Pipelines guideline categories in read-only mode.
        Call list_categories and briefly describe the scope of each returned category.
        Do not propose file changes.
        """;
}
