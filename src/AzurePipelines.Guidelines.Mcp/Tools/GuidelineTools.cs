using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using AzurePipelines.Guidelines.Core;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace AzurePipelines.Guidelines.Mcp.Tools;

/// <summary>
/// MCP tool handlers for browsing and querying Azure Pipelines guideline definitions.
/// </summary>
[McpServerToolType]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance", "CA1812:Avoid uninstantiated internal classes",
    Justification = "Instantiated by the MCP SDK via dependency injection.")]
internal sealed class GuidelineTools(
    IGuidelineRepository repository,
    IGuidelineAutomationMetadataProvider? automationMetadataProvider = null,
    ILogger<GuidelineTools>? logger = null)
{
    // Compact JSON with camel-case property names. Null values are omitted so AI clients
    // receive smaller responses and the shared contract stays predictable.
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    // ── list_guidelines ───────────────────────────────────────────────────────

    /// <summary>
    /// Lists all loaded Azure Pipelines guidelines (id, title, category, severity).
    /// Returns a JSON array. Optionally filter by category.
    /// </summary>
    [McpServerTool(Name = "list_guidelines", Title = "List guidelines", ReadOnly = true)]
    [Description(
        "Lists all Azure Pipelines guidelines. " +
        "Returns a JSON array with id, title, category, and severity for each guideline. " +
        "Optionally filter by category (general|jobs|parameters|pipelines|stages|steps|variables).")]
    internal string ListGuidelines(
        [Description(
            "Optional category filter. " +
            "Allowed values: general, jobs, parameters, pipelines, stages, steps, variables. " +
            "Omit or pass null to return all categories.")]
        string? category = null)
    {
        McpToolInvocationLog.Log(logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<GuidelineTools>.Instance,
            "list_guidelines", category: category);

        IReadOnlyList<GuidelineDefinition> guidelines;

        if (category is null)
        {
            guidelines = repository.GetAll();
        }
        else if (TryParseCategory(category, out GuidelineCategory parsed))
        {
            guidelines = repository.GetByCategory(parsed);
        }
        else
        {
            return JsonSerializer.Serialize(
                new ErrorResponseDto($"Unknown category '{category}'. " +
                    "Allowed values: general, jobs, parameters, pipelines, stages, steps, variables."),
                _jsonOptions);
        }

        GuidelineSummaryDto[] summaries = new GuidelineSummaryDto[guidelines.Count];
        for (int i = 0; i < guidelines.Count; i++)
        {
            GuidelineDefinition g = guidelines[i];
            summaries[i] = new GuidelineSummaryDto(
                g.Id.Value,
                g.Title,
                EnumToJsonString(g.Category),
                EnumToJsonString(g.Severity));
        }

        return JsonSerializer.Serialize(summaries, _jsonOptions);
    }

    // ── get_guideline ─────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the details of a single guideline by its ID.
    /// </summary>
    [McpServerTool(Name = "get_guideline", Title = "Get guideline details", ReadOnly = true)]
    [Description(
        "Returns the details of a single Azure Pipelines guideline by its stable ID " +
        "(e.g. ADOG-STEPS-001). By default this returns a compact summary with id, title, category, and severity. " +
        "Pass detail=full to include description, detection hints, fix guidance, and reference links.")]
    internal string GetGuideline(
        [Description("The stable guideline identifier, e.g. ADOG-STEPS-001.")]
        string id,
        [Description("Optional detail level. Use 'summary' for the compact response or 'full' for the detailed response. Defaults to 'summary'.")]
        string? detail = null)
    {
        McpToolInvocationLog.Log(logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<GuidelineTools>.Instance,
            "get_guideline");

        if (string.IsNullOrWhiteSpace(id))
        {
            return JsonSerializer.Serialize(
                new ErrorResponseDto("Parameter 'id' is required."), _jsonOptions);
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
                    $"'{id}' is not a valid guideline ID. " +
                    "Expected format: ADOG-{CATEGORY}-{NNN}, e.g. ADOG-STEPS-001."),
                _jsonOptions);
        }

        GuidelineDefinition? guideline = repository.FindById(guidelineId);

        if (guideline is null)
        {
            return JsonSerializer.Serialize(
                new ErrorResponseDto($"Guideline '{id}' not found."), _jsonOptions);
        }

        if (string.Equals(detail, "full", StringComparison.OrdinalIgnoreCase))
        {
            return JsonSerializer.Serialize(ToDetailDto(guideline, automationMetadataProvider), _jsonOptions);
        }

        if (!string.IsNullOrWhiteSpace(detail) && !string.Equals(detail, "summary", StringComparison.OrdinalIgnoreCase))
        {
            return JsonSerializer.Serialize(
                new ErrorResponseDto("Parameter 'detail' must be either 'summary' or 'full'."), _jsonOptions);
        }

        return JsonSerializer.Serialize(
            new GuidelineSummaryDto(guideline.Id.Value, guideline.Title, EnumToJsonString(guideline.Category), EnumToJsonString(guideline.Severity)),
            _jsonOptions);
    }

    // ── search_guidelines ─────────────────────────────────────────────────────

    /// <summary>
    /// Searches guideline titles and descriptions for a keyword.
    /// </summary>
    [McpServerTool(Name = "search_guidelines", Title = "Search guidelines", ReadOnly = true)]
    [Description(
        "Searches Azure Pipelines guidelines whose title or description contains the given " +
        "keyword (case-insensitive). Returns a JSON array with id, title, category, and severity.")]
    internal string SearchGuidelines(
        [Description("The keyword to search for in guideline titles and descriptions.")]
        string keyword)
    {
        McpToolInvocationLog.Log(logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<GuidelineTools>.Instance,
            "search_guidelines");

        if (string.IsNullOrWhiteSpace(keyword))
        {
            return JsonSerializer.Serialize(
                new ErrorResponseDto("Parameter 'keyword' is required."), _jsonOptions);
        }

        IReadOnlyList<GuidelineDefinition> all = repository.GetAll();
        List<GuidelineSummaryDto> matches = [];

        foreach (GuidelineDefinition g in all)
        {
            if (g.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                g.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                matches.Add(new GuidelineSummaryDto(
                    g.Id.Value,
                    g.Title,
                    EnumToJsonString(g.Category),
                    EnumToJsonString(g.Severity)));
            }
        }

        return JsonSerializer.Serialize(matches, _jsonOptions);
    }

    // ── list_categories ───────────────────────────────────────────────────────

    /// <summary>
    /// Returns a summary of available guideline categories and their counts.
    /// </summary>
    [McpServerTool(Name = "list_categories", Title = "List guideline categories", ReadOnly = true)]
    [Description(
        "Returns a JSON array listing each guideline category and the number of guidelines " +
        "it contains. Useful for exploring what the server knows before calling list_guidelines.")]
    internal string ListCategories()
    {
        McpToolInvocationLog.Log(logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<GuidelineTools>.Instance,
            "list_categories");

        IReadOnlyList<GuidelineDefinition> all = repository.GetAll();
        Dictionary<string, int> counts = new(StringComparer.Ordinal);

        foreach (GuidelineDefinition g in all)
        {
            string key = EnumToJsonString(g.Category);
            counts[key] = counts.TryGetValue(key, out int current) ? current + 1 : 1;
        }

        CategoryCountDto[] result = new CategoryCountDto[counts.Count];
        int idx = 0;
        foreach (KeyValuePair<string, int> kv in counts)
        {
            result[idx++] = new CategoryCountDto(kv.Key, kv.Value);
        }

        Array.Sort(result, static (a, b) =>
            string.Compare(a.Category, b.Category, StringComparison.Ordinal));

        return JsonSerializer.Serialize(result, _jsonOptions);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool TryParseCategory(string value, out GuidelineCategory result)
    {
        result = value.ToUpperInvariant() switch
        {
            "GENERAL" => GuidelineCategory.General,
            "JOBS" => GuidelineCategory.Jobs,
            "PARAMETERS" => GuidelineCategory.Parameters,
            "PIPELINES" => GuidelineCategory.Pipelines,
            "STAGES" => GuidelineCategory.Stages,
            "STEPS" => GuidelineCategory.Steps,
            "VARIABLES" => GuidelineCategory.Variables,
            _ => (GuidelineCategory)(-1),
        };

        return (int)result >= 0;
    }

    private static GuidelineDetailDto ToDetailDto(
        GuidelineDefinition g,
        IGuidelineAutomationMetadataProvider? automationMetadataProvider)
    {
        DetectionHintDto[]? hints = g.DetectionHints.Count > 0
            ? BuildHintDtos(g.DetectionHints)
            : null;

        FixDto? fix = g.Fix is not null
            ? new FixDto(g.Fix.Summary, g.Fix.Before, g.Fix.After)
            : null;

        return new GuidelineDetailDto(
            g.Id.Value,
            g.Title,
            EnumToJsonString(g.Category),
            EnumToJsonString(g.Severity),
            g.Description,
            g.Rationale,
            g.Tags.Count > 0 ? [.. g.Tags] : null,
            hints,
            fix,
            g.References.Count > 0 ? [.. g.References] : null,
            GetAutomationStatus(g, automationMetadataProvider),
            GetAutomationReason(g, automationMetadataProvider));
    }

    private static string GetAutomationStatus(
        GuidelineDefinition guideline,
        IGuidelineAutomationMetadataProvider? automationMetadataProvider) =>
        EnumToJsonString(automationMetadataProvider?.GetAutomationMetadata(guideline.Id)?.Status ?? GuidelineAutomationStatus.NotAutomatable);

    private static string GetAutomationReason(
        GuidelineDefinition guideline,
        IGuidelineAutomationMetadataProvider? automationMetadataProvider) =>
        automationMetadataProvider?.GetAutomationMetadata(guideline.Id)?.Reason ?? "No local automation metadata is available.";

    // Converts an enum value to a lowercase ASCII string for JSON output.
    // We avoid string.ToLowerInvariant because the codebase treats enum names as stable
    // ASCII identifiers, and CA1308 warns against ToLowerInvariant in invariant contexts.
    private static string EnumToJsonString<T>(T value) where T : struct, Enum
    {
        string name = value.ToString();
        return string.Create(name.Length, name, static (span, src) =>
        {
            for (int i = 0; i < src.Length; i++)
            {
                char c = src[i];
                span[i] = c is >= 'A' and <= 'Z' ? (char)(c + 32) : c;
            }
        });
    }

    // ── Internal DTOs ─────────────────────────────────────────────────────────

    private static GuidelineSummaryDto CreateSummary(GuidelineDefinition guideline) =>
        new(
            guideline.Id.Value,
            guideline.Title,
            EnumToJsonString(guideline.Category),
            EnumToJsonString(guideline.Severity));

    private static GuidelineDetailDto CreateDetail(GuidelineDefinition guideline) =>
        new(
            guideline.Id.Value,
            guideline.Title,
            EnumToJsonString(guideline.Category),
            EnumToJsonString(guideline.Severity),
            guideline.Description,
            guideline.Rationale,
            guideline.Tags.Count > 0 ? [.. guideline.Tags] : null,
            BuildHintDtos(guideline.DetectionHints),
            BuildFixDto(guideline.Fix),
            guideline.References.Count > 0 ? [.. guideline.References] : null,
            EnumToJsonString(GuidelineAutomationStatus.NotAutomatable),
            "No local automation metadata is available.");

    private static FixDto? BuildFixDto(FixGuidance? fix) =>
        fix is null ? null : new FixDto(fix.Summary, fix.Before, fix.After);

    private static DetectionHintDto[] BuildHintDtos(IReadOnlyList<DetectionHint> hints)
    {
        DetectionHintDto[] result = new DetectionHintDto[hints.Count];
        for (int i = 0; i < hints.Count; i++)
        {
            DetectionHint h = hints[i];
            result[i] = new DetectionHintDto(
                EnumToJsonString(h.Kind),
                EnumToJsonString(h.Scope),
                h.Expression,
                h.Description);
        }

        return result;
    }

    private static GuidelineSummaryDto[] BuildSummaryDtos(IReadOnlyList<GuidelineDefinition> guidelines)
    {
        GuidelineSummaryDto[] summaries = new GuidelineSummaryDto[guidelines.Count];
        for (int i = 0; i < guidelines.Count; i++)
        {
            summaries[i] = CreateSummary(guidelines[i]);
        }

        return summaries;
    }

    private static GuidelineSummaryDto[] BuildSummaryDtos(IEnumerable<GuidelineDefinition> guidelines)
    {
        List<GuidelineSummaryDto> summaries = [];
        foreach (GuidelineDefinition guideline in guidelines)
        {
            summaries.Add(CreateSummary(guideline));
        }

        return [.. summaries];
    }

    private static GuidelineSummaryDto[] BuildSummaryDtos(IReadOnlyCollection<GuidelineDefinition> guidelines)
    {
        GuidelineSummaryDto[] summaries = new GuidelineSummaryDto[guidelines.Count];
        int index = 0;
        foreach (GuidelineDefinition guideline in guidelines)
        {
            summaries[index++] = CreateSummary(guideline);
        }

        return summaries;
    }
}
