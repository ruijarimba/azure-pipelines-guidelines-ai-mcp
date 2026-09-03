using System.ComponentModel;
using System.Text.Json;
using AzurePipelines.Guidelines.Core;
using ModelContextProtocol.Server;

namespace AzurePipelines.Guidelines.Mcp.Resources;

/// <summary>
/// MCP resource handler that returns the cache version for the Azure Pipelines YAML guideline catalogue.
/// </summary>
[McpServerResourceType]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance", "CA1812:Avoid uninstantiated internal classes",
    Justification = "Instantiated by the MCP SDK via dependency injection.")]
internal sealed class GuidelineCatalogueVersionResource(IGuidelineRepository repository)
{
    /// <summary>
    /// Returns a small cache fingerprint for the current guideline catalogue.
    /// </summary>
    [McpServerResource(
        UriTemplate = "adog://guidelines/version",
        Name = "guidelines-version",
        Title = "Azure Pipelines YAML guideline catalogue version",
        MimeType = "application/json")]
    [Description(
        "Returns the current Azure Pipelines YAML guideline catalogue version. Clients can use this " +
        "small cache key to skip refetching the full catalogue when the version is unchanged.")]
    internal Task<string> GetCatalogueVersionAsync()
    {
        string response = JsonSerializer.Serialize(new CatalogueVersionResponseDto(repository.ContentVersion));
        return Task.FromResult(response);
    }
}
