using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using AzurePipelines.Guidelines.Core;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace AzurePipelines.Guidelines.Mcp.Tools;

/// <summary>
/// MCP tool handler that explains a single Azure Pipelines guideline diagnostic in focused detail.
/// </summary>
[McpServerToolType]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance", "CA1812:Avoid uninstantiated internal classes",
    Justification = "Instantiated by the MCP SDK via dependency injection.")]
internal sealed class ExplainDiagnosticTool(
    IGuidelineRepository repository,
    IGuidelineAutomationMetadataProvider? automationMetadataProvider = null,
    ILogger<ExplainDiagnosticTool>? logger = null)
{
    // Compact JSON with camel-case property names. Null values are omitted so AI clients
    // receive smaller responses and the shared contract stays predictable.
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Explains a single guideline in full detail, optionally echoing back the diagnostic
    /// context (message, file path, line, and column) that triggered the explanation request.
    /// </summary>
    [McpServerTool(
        Name = "explain_diagnostic",
        Title = "Explain an Azure Pipelines YAML diagnostic",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description(
        "Explains one Azure Pipelines YAML pipeline or template diagnostic in focused detail: guideline " +
        "purpose, recommendation strength, detection hints, fix guidance, references, and automation metadata. " +
        "Optionally echoes the diagnostic message, file path, line, and column for context. Returns only " +
        "the requested guideline, never the full catalogue.")]
    internal string ExplainDiagnostic(
        [Description("The stable guideline identifier the diagnostic was raised for, e.g. ADOG-STEPS-001.")]
        string guidelineId,
        [Description("Optional diagnostic message text to echo back for context.")]
        string? message = null,
        [Description("Optional file path where the diagnostic was found, to echo back for context.")]
        string? filePath = null,
        [Description("Optional one-based line number where the diagnostic was found.")]
        int? line = null,
        [Description("Optional one-based column number where the diagnostic was found.")]
        int? column = null)
    {
        McpToolInvocationLog.Log(
            logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ExplainDiagnosticTool>.Instance,
            "explain_diagnostic");

        if (string.IsNullOrWhiteSpace(guidelineId))
        {
            return JsonSerializer.Serialize(
                new ErrorResponseDto("Parameter 'guidelineId' is required."), _jsonOptions);
        }

        GuidelineId parsedId;
        try
        {
            parsedId = new GuidelineId(guidelineId);
        }
        catch (ArgumentException)
        {
            return JsonSerializer.Serialize(
                new ErrorResponseDto(
                    $"'{guidelineId}' is not a valid guideline ID. " +
                    "Expected format: ADOG-{CATEGORY}-{NNN}, e.g. ADOG-STEPS-001."),
                _jsonOptions);
        }

        GuidelineDefinition? guideline = repository.FindById(parsedId);
        if (guideline is null)
        {
            return JsonSerializer.Serialize(
                new ErrorResponseDto($"Guideline '{guidelineId}' not found."), _jsonOptions);
        }

        DiagnosticContextDto? context = BuildContext(message, filePath, line, column);
        DiagnosticExplanationDto explanation = new(
            GuidelineToolSupport.ToDetailDto(guideline, automationMetadataProvider), context);

        return JsonSerializer.Serialize(explanation, _jsonOptions);
    }

    private static DiagnosticContextDto? BuildContext(string? message, string? filePath, int? line, int? column) =>
        message is null && filePath is null && line is null && column is null
            ? null
            : new DiagnosticContextDto(message, filePath, line, column);
}
