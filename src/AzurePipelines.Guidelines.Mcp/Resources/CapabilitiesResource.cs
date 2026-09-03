using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using AzurePipelines.Guidelines.Core;
using ModelContextProtocol.Server;

namespace AzurePipelines.Guidelines.Mcp.Resources;

/// <summary>
/// MCP resource handler that describes the Azure Pipelines YAML guideline server capabilities.
/// </summary>
[McpServerResourceType]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance", "CA1812:Avoid uninstantiated internal classes",
    Justification = "Instantiated by the MCP SDK via dependency injection.")]
internal sealed class CapabilitiesResource(IGuidelineRepository repository)
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Returns the cacheable MCP surface and current catalogue version.
    /// </summary>
    [McpServerResource(
        UriTemplate = "adog://capabilities",
        Name = "capabilities",
        Title = "Azure Pipelines YAML guideline server capabilities",
        MimeType = "application/json")]
    [Description(
        "Describes this Azure Pipelines YAML guideline server, including its purpose, version, " +
        "catalogue version, supported transports, and available tools, resources, and prompts.")]
    internal Task<string> GetCapabilitiesAsync()
    {
        CapabilitiesResponseDto capabilities = new(
            "azure-pipelines-guidelines",
            "Azure Pipelines YAML Guidelines",
            "Deterministic analysis and guideline lookup for Azure Pipelines YAML pipelines and reusable templates.",
            "https://github.com/ruijarimba/azure-pipelines-guidelines-ai-mcp",
            "1.0.0",
            repository.ContentVersion,
            ["stdio", "streamable-http"],
            [
                new McpCapabilityDescriptorDto(
                    "analyze_template_or_folder",
                    "Analyze Azure Pipelines YAML pipelines and templates",
                    "Analyzes inline YAML, pipeline files, reusable templates, or a directory against loaded coding guidelines."),
                new McpCapabilityDescriptorDto(
                    "list_guidelines",
                    "Browse Azure Pipelines YAML guidelines",
                    "Lists concise Azure Pipelines YAML coding guideline summaries."),
                new McpCapabilityDescriptorDto(
                    "get_guideline",
                    "Get an Azure Pipelines YAML guideline",
                    "Gets summary or full detail for one coding guideline by stable ID."),
                new McpCapabilityDescriptorDto(
                    "search_guidelines",
                    "Search Azure Pipelines YAML guidelines",
                    "Finds coding guidelines by keyword in their titles and descriptions."),
                new McpCapabilityDescriptorDto(
                    "list_categories",
                    "Browse Azure Pipelines YAML guideline categories",
                    "Lists supported coding guideline categories and their counts."),
                new McpCapabilityDescriptorDto(
                    "explain_diagnostic",
                    "Explain an Azure Pipelines YAML diagnostic",
                    "Explains focused guideline detail and remediation for one diagnostic.")
            ],
            [
                new McpCapabilityDescriptorDto(
                    "adog://capabilities",
                    "Azure Pipelines YAML guideline server capabilities",
                    "Describes this server's purpose and its available MCP capabilities."),
                new McpCapabilityDescriptorDto(
                    "adog://guidelines",
                    "Azure Pipelines YAML guideline catalogue",
                    "Returns the complete coding guideline catalogue."),
                new McpCapabilityDescriptorDto(
                    "adog://guidelines/version",
                    "Azure Pipelines YAML guideline catalogue version",
                    "Returns a small cache key for the current guideline catalogue."),
                new McpCapabilityDescriptorDto(
                    "adog://guidelines/category/{category}",
                    "Azure Pipelines YAML guideline catalogue by category",
                    "Returns coding guidelines for one category."),
                new McpCapabilityDescriptorDto(
                    "adog://guidelines/{id}",
                    "Azure Pipelines YAML guideline detail",
                    "Returns full detail for one coding guideline."),
                new McpCapabilityDescriptorDto(
                    "adog://guidelines/{id}/automation",
                    "Azure Pipelines YAML guideline automation metadata",
                    "Returns automation status and rationale for one coding guideline.")
            ],
            [
                new McpCapabilityDescriptorDto(
                    "review",
                    "Review Azure Pipelines YAML pipelines and templates",
                    "Guides a read-only review of inline YAML, one file, or a directory."),
                new McpCapabilityDescriptorDto(
                    "review-summary",
                    "Summarize Azure Pipelines YAML guideline violations",
                    "Guides a repository-wide, summary-only guideline review."),
                new McpCapabilityDescriptorDto(
                    "review-category",
                    "Review one Azure Pipelines YAML guideline category",
                    "Guides analysis for one coding guideline category."),
                new McpCapabilityDescriptorDto(
                    "review-guideline",
                    "Review selected Azure Pipelines YAML guidelines",
                    "Guides analysis for selected coding guideline IDs."),
                new McpCapabilityDescriptorDto(
                    "explain-guideline",
                    "Explain an Azure Pipelines YAML guideline",
                    "Guides a detailed explanation for one coding guideline."),
                new McpCapabilityDescriptorDto(
                    "find-guidelines",
                    "Search Azure Pipelines YAML guidelines",
                    "Guides keyword search across the coding guideline catalogue."),
                new McpCapabilityDescriptorDto(
                    "list-guidelines",
                    "Browse Azure Pipelines YAML guidelines",
                    "Guides browsing guideline summaries by category."),
                new McpCapabilityDescriptorDto(
                    "list-categories",
                    "Browse Azure Pipelines YAML guideline categories",
                    "Guides exploration of the available coding guideline categories.")
            ],
            new CapabilitiesSupportDto(AutomationMetadata: true, Prompts: true));

        return Task.FromResult(JsonSerializer.Serialize(capabilities, _jsonOptions));
    }
}
