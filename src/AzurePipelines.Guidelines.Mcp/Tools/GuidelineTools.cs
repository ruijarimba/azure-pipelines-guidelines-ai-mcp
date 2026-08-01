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
                new ErrorResponse($"Unknown category '{category}'. " +
                    "Allowed values: general, jobs, parameters, pipelines, stages, steps, variables."),
                _jsonOptions);
        }

        GuidelineSummary[] summaries = new GuidelineSummary[guidelines.Count];
        for (int i = 0; i < guidelines.Count; i++)
        {
            GuidelineDefinition g = guidelines[i];
            summaries[i] = new GuidelineSummary(
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
                new ErrorResponse("Parameter 'id' is required."), _jsonOptions);
        }

        GuidelineId guidelineId;
        try
        {
            guidelineId = new GuidelineId(id);
        }
        catch (ArgumentException)
        {
            return JsonSerializer.Serialize(
                new ErrorResponse(
                    $"'{id}' is not a valid guideline ID. " +
                    "Expected format: ADOG-{CATEGORY}-{NNN}, e.g. ADOG-STEPS-001."),
                _jsonOptions);
        }

        GuidelineDefinition? guideline = repository.FindById(guidelineId);

        if (guideline is null)
        {
            return JsonSerializer.Serialize(
                new ErrorResponse($"Guideline '{id}' not found."), _jsonOptions);
        }

        if (string.Equals(detail, "full", StringComparison.OrdinalIgnoreCase))
        {
            return JsonSerializer.Serialize(ToDetailDto(guideline), _jsonOptions);
        }

        if (!string.IsNullOrWhiteSpace(detail) && !string.Equals(detail, "summary", StringComparison.OrdinalIgnoreCase))
        {
            return JsonSerializer.Serialize(
                new ErrorResponse("Parameter 'detail' must be either 'summary' or 'full'."), _jsonOptions);
        }

        return JsonSerializer.Serialize(
            new GuidelineSummary(guideline.Id.Value, guideline.Title, EnumToJsonString(guideline.Category), EnumToJsonString(guideline.Severity)),
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
                new ErrorResponse("Parameter 'keyword' is required."), _jsonOptions);
        }

        IReadOnlyList<GuidelineDefinition> all = repository.GetAll();
        List<GuidelineSummary> matches = [];

        foreach (GuidelineDefinition g in all)
        {
            if (g.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                g.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                matches.Add(new GuidelineSummary(
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

        CategoryCount[] result = new CategoryCount[counts.Count];
        int idx = 0;
        foreach (KeyValuePair<string, int> kv in counts)
        {
            result[idx++] = new CategoryCount(kv.Key, kv.Value);
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

    private static GuidelineDetailDto ToDetailDto(GuidelineDefinition g)
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
            g.References.Count > 0 ? [.. g.References] : null);
    }

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

    private sealed record GuidelineSummary(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("category")] string Category,
        [property: JsonPropertyName("severity")] string Severity);

    private sealed record CategoryCount(
        [property: JsonPropertyName("category")] string Category,
        [property: JsonPropertyName("count")] int Count);

    private sealed record ErrorResponse(
        [property: JsonPropertyName("error")] string Error);

    private sealed record GuidelineDetailDto(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("category")] string Category,
        [property: JsonPropertyName("severity")] string Severity,
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("rationale")] string? Rationale,
        [property: JsonPropertyName("tags")] string[]? Tags,
        [property: JsonPropertyName("detectionHints")] DetectionHintDto[]? DetectionHints,
        [property: JsonPropertyName("fix")] FixDto? Fix,
        [property: JsonPropertyName("references")] string[]? References);

    private sealed record DetectionHintDto(
        [property: JsonPropertyName("kind")] string Kind,
        [property: JsonPropertyName("scope")] string Scope,
        [property: JsonPropertyName("expression")] string? Expression,
        [property: JsonPropertyName("description")] string Description);

    private sealed record FixDto(
        [property: JsonPropertyName("summary")] string Summary,
        [property: JsonPropertyName("before")] string? Before,
        [property: JsonPropertyName("after")] string? After);
}
