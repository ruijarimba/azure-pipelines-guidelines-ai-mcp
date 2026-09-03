using System.ComponentModel;
using System.Text.Json;
using AzurePipelines.Guidelines.Core;
using ModelContextProtocol.Server;

namespace AzurePipelines.Guidelines.Mcp.Tools;

/// <summary>
/// MCP tool that returns a guideline summary or full guideline details.
/// </summary>
[McpServerToolType]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by the MCP SDK via dependency injection.")]
internal sealed class GetGuidelineTool(
    IGuidelineRepository repository,
    IGuidelineAutomationMetadataProvider? automationMetadataProvider = null,
    Microsoft.Extensions.Logging.ILogger<GetGuidelineTool>? logger = null)
{
    /// <summary>
    /// Returns a guideline by its stable identifier.
    /// </summary>
    [McpServerTool(Name = "get_guideline", Title = "Get an Azure Pipelines YAML guideline", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Gets one Azure Pipelines YAML coding guideline by stable ID, such as ADOG-STEPS-001. Returns a compact summary by default. Pass detail=full for the description, detection hints, fix guidance, and reference links.")]
    internal string GetGuideline(
        [Description("The stable guideline identifier, e.g. ADOG-STEPS-001.")] string id,
        [Description("Optional detail level. Use 'summary' for the compact response or 'full' for the detailed response. Defaults to 'summary'.")] string? detail = null)
    {
        McpToolInvocationLog.Log(
            logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<GetGuidelineTool>.Instance,
            "get_guideline");
        if (string.IsNullOrWhiteSpace(id))
        {
            return JsonSerializer.Serialize(
                new ErrorResponseDto("Parameter 'id' is required."),
                GuidelineToolSupport.JsonOptions);
        }

        GuidelineId guidelineId;

        try
        {
            guidelineId = new GuidelineId(id);
        }
        catch (ArgumentException)
        {
            return JsonSerializer.Serialize(
                new ErrorResponseDto(
                    $"'{id}' is not a valid guideline ID. Expected format: ADOG-{{CATEGORY}}-{{NNN}}, e.g. ADOG-STEPS-001."),
                GuidelineToolSupport.JsonOptions);
        }

        GuidelineDefinition? guideline = repository.FindById(guidelineId);

        if (guideline is null)
        {
            return JsonSerializer.Serialize(
                new ErrorResponseDto($"Guideline '{id}' not found."),
                GuidelineToolSupport.JsonOptions);
        }

        if (string.Equals(detail, "full", StringComparison.OrdinalIgnoreCase))
        {
            return JsonSerializer.Serialize(GuidelineToolSupport.ToDetailDto(guideline, automationMetadataProvider), GuidelineToolSupport.JsonOptions);
        }

        if (!string.IsNullOrWhiteSpace(detail) &&
            !string.Equals(detail, "summary", StringComparison.OrdinalIgnoreCase))
        {
            return JsonSerializer.Serialize(new ErrorResponseDto("Parameter 'detail' must be either 'summary' or 'full'."), GuidelineToolSupport.JsonOptions);
        }
        return JsonSerializer.Serialize(GuidelineToolSupport.CreateSummary(guideline), GuidelineToolSupport.JsonOptions);
    }
}
