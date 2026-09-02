using System.ComponentModel;
using System.Text.Json;
using AzurePipelines.Guidelines.Core;
using ModelContextProtocol.Server;

namespace AzurePipelines.Guidelines.Mcp.Tools;

/// <summary>
/// MCP tool that lists guideline categories and their guideline counts.
/// </summary>
[McpServerToolType]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by the MCP SDK via dependency injection.")]
internal sealed class ListCategoriesTool(
    IGuidelineRepository repository,
    Microsoft.Extensions.Logging.ILogger<ListCategoriesTool>? logger = null)
{
    /// <summary>
    /// Returns the available categories and their counts.
    /// </summary>
    [McpServerTool(Name = "list_categories", Title = "List guideline categories", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Returns a JSON array listing each guideline category and the number of guidelines it contains. Useful for exploring what the server knows before calling list_guidelines.")]
    internal string ListCategories()
    {
        McpToolInvocationLog.Log(
            logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ListCategoriesTool>.Instance,
            "list_categories");
        Dictionary<string, int> counts = new(StringComparer.Ordinal);
        foreach (GuidelineDefinition guideline in repository.GetAll())
        {
            string key = GuidelineToolSupport.ToJsonString(guideline.Category);
            counts[key] = counts.TryGetValue(key, out int current) ? current + 1 : 1;
        }
        CategoryCountDto[] result = counts.Select(pair => new CategoryCountDto(pair.Key, pair.Value)).ToArray();
        Array.Sort(result, static (left, right) => string.Compare(left.Category, right.Category, StringComparison.Ordinal));
        return JsonSerializer.Serialize(result, GuidelineToolSupport.JsonOptions);
    }
}
