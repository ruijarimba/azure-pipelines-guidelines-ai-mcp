using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using AzurePipelines.Guidelines.Core;
using ModelContextProtocol.Server;

namespace AzurePipelines.Guidelines.Mcp.Resources;

/// <summary>
/// MCP resource handlers that expose the Azure Pipelines guideline catalogue
/// as readable resources clients can subscribe to and fetch directly.
/// </summary>
[McpServerResourceType]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance", "CA1812:Avoid uninstantiated internal classes",
    Justification = "Instantiated by the MCP SDK via dependency injection.")]
internal sealed class GuidelineResources(IGuidelineRepository repository)
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    // ── adog://guidelines ─────────────────────────────────────────────────────

    /// <summary>
    /// Returns the full guideline catalogue as a JSON array.
    /// Each element contains id, title, category, and severity.
    /// </summary>
    [McpServerResource(
        UriTemplate = "adog://guidelines",
        Name = "guidelines",
        MimeType = "application/json")]
    [Description(
        "The complete Azure Pipelines guideline catalogue. " +
        "Returns a JSON array; each element contains id, title, category, and severity.")]
    internal Task<string> GetAllGuidelinesAsync()
    {
        IReadOnlyList<GuidelineDefinition> all = repository.GetAll();

        GuidelineSummary[] summaries = new GuidelineSummary[all.Count];
        for (int i = 0; i < all.Count; i++)
        {
            GuidelineDefinition g = all[i];
            summaries[i] = new GuidelineSummary(
                g.Id.Value,
                g.Title,
                EnumToJsonString(g.Category),
                EnumToJsonString(g.Severity));
        }

        return Task.FromResult(JsonSerializer.Serialize(summaries, _jsonOptions));
    }

    // ── adog://guidelines/{id} ────────────────────────────────────────────────

    /// <summary>
    /// Returns the full detail for a single guideline identified by its stable ID.
    /// </summary>
    [McpServerResource(
        UriTemplate = "adog://guidelines/{id}",
        Name = "guideline",
        MimeType = "application/json")]
    [Description(
        "Full details for a single Azure Pipelines guideline. " +
        "Supply the stable guideline ID as the {id} path segment, e.g. adog://guidelines/ADOG-STEPS-001. " +
        "Returns a JSON object with id, title, category, severity, description, rationale, " +
        "detectionHints, fix, and references.")]
    internal Task<string> GetGuidelineAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Task.FromResult(
                JsonSerializer.Serialize(new ErrorResponse("Path segment 'id' is required."), _jsonOptions));
        }

        GuidelineId guidelineId;
        try
        {
            guidelineId = new GuidelineId(id);
        }
        catch (ArgumentException)
        {
            return Task.FromResult(
                JsonSerializer.Serialize(
                    new ErrorResponse(
                        $"'{id}' is not a valid guideline ID. " +
                        "Expected format: ADOG-{CATEGORY}-{NNN}, e.g. ADOG-STEPS-001."),
                    _jsonOptions));
        }

        GuidelineDefinition? guideline = repository.FindById(guidelineId);

        if (guideline is null)
        {
            return Task.FromResult(
                JsonSerializer.Serialize(new ErrorResponse($"Guideline '{id}' not found."), _jsonOptions));
        }

        return Task.FromResult(JsonSerializer.Serialize(ToDetailDto(guideline), _jsonOptions));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

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
    // Avoids CA1308 (ToLowerInvariant) by using char arithmetic on ASCII enum names.
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
