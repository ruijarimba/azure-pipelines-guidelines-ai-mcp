using System.ComponentModel;
using System.Text.Json;
using AzurePipelines.Guidelines.Core;
using ModelContextProtocol.Server;

namespace AzurePipelines.Guidelines.Mcp.Tools;

/// <summary>
/// MCP tool that lists guideline summaries, optionally filtered by category.
/// </summary>
[McpServerToolType]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by the MCP SDK via dependency injection.")]
internal sealed class ListGuidelinesTool(
    IGuidelineRepository repository,
    Microsoft.Extensions.Logging.ILogger<ListGuidelinesTool>? logger = null)
{
    /// <summary>
    /// Returns the available guideline summaries.
    /// </summary>
    [McpServerTool(Name = "list_guidelines", Title = "List guidelines", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Lists all Azure Pipelines guidelines. Returns a JSON array with id, title, category, and severity. Optionally filter by category (general|jobs|parameters|pipelines|stages|steps|variables).")]
    internal string ListGuidelines(
        [Description("Optional category filter. Allowed values: general, jobs, parameters, pipelines, stages, steps, variables. Omit or pass null to return all categories.")]
        string? category = null)
    {
        McpToolInvocationLog.Log(
            logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ListGuidelinesTool>.Instance,
            "list_guidelines",
            category: category);
        IReadOnlyList<GuidelineDefinition> guidelines;
        if (category is null)
        {
            guidelines = repository.GetAll();
        }
        else if (GuidelineToolSupport.TryParseCategory(category, out GuidelineCategory parsed))
        {
            guidelines = repository.GetByCategory(parsed);
        }
        else
        {
            return JsonSerializer.Serialize(
                new ErrorResponseDto(
                    $"Unknown category '{category}'. Allowed values: general, jobs, parameters, pipelines, stages, steps, variables."),
                GuidelineToolSupport.JsonOptions);
        }

        return JsonSerializer.Serialize(
            guidelines.Select(GuidelineToolSupport.CreateSummary).ToArray(),
            GuidelineToolSupport.JsonOptions);
    }
}
