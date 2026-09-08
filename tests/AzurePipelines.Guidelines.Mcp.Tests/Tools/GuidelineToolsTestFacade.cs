using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Mcp.Tools;

namespace AzurePipelines.Guidelines.Mcp.Tests.Tools;

/// <summary>
/// Test-only facade that composes the dedicated guideline catalogue tools.
/// </summary>
internal sealed class GuidelineTools
{
    private readonly ListGuidelinesTool _listGuidelines;
    private readonly GetGuidelineTool _getGuideline;
    private readonly SearchGuidelinesTool _searchGuidelines;
    private readonly ListCategoriesTool _listCategories;

    public GuidelineTools(
        IGuidelineRepository repository,
        IGuidelineAutomationMetadataProvider? automationMetadataProvider = null)
    {
        _listGuidelines = new ListGuidelinesTool(repository);
        _getGuideline = new GetGuidelineTool(repository, automationMetadataProvider);
        _searchGuidelines = new SearchGuidelinesTool(repository);
        _listCategories = new ListCategoriesTool(repository);
    }

    public string ListGuidelines(string? category = null) =>
        _listGuidelines.ListGuidelines(category);

    public string GetGuideline(string id, string? detail = null) =>
        _getGuideline.GetGuideline(id, detail);

    public string SearchGuidelines(string keyword) =>
        _searchGuidelines.SearchGuidelines(keyword);

    public string ListCategories() => _listCategories.ListCategories();
}
