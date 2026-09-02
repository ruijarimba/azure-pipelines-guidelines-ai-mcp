using System.ComponentModel;
using System.Text.Json;
using AzurePipelines.Guidelines.Core;
using ModelContextProtocol.Server;

namespace AzurePipelines.Guidelines.Mcp.Tools;

/// <summary>
/// MCP tool that searches guideline titles and descriptions.
/// </summary>
[McpServerToolType]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by the MCP SDK via dependency injection.")]
internal sealed class SearchGuidelinesTool(
    IGuidelineRepository repository,
    Microsoft.Extensions.Logging.ILogger<SearchGuidelinesTool>? logger = null)
{
    /// <summary>
    /// Returns guidelines matching a keyword.
    /// </summary>
    [McpServerTool(Name = "search_guidelines", Title = "Search guidelines", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Searches Azure Pipelines guidelines whose title or description contains the given keyword (case-insensitive). Returns a JSON array with id, title, category, and severity.")]
    internal string SearchGuidelines([Description("The keyword to search for in guideline titles and descriptions.")] string keyword)
    {
        McpToolInvocationLog.Log(
            logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SearchGuidelinesTool>.Instance,
            "search_guidelines");
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return JsonSerializer.Serialize(new ErrorResponseDto("Parameter 'keyword' is required."), GuidelineToolSupport.JsonOptions);
        }
        GuidelineSummaryDto[] matches = repository.GetAll()
            .Where(g =>
                g.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                g.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .Select(GuidelineToolSupport.CreateSummary)
            .ToArray();

        return JsonSerializer.Serialize(matches, GuidelineToolSupport.JsonOptions);
    }
}
