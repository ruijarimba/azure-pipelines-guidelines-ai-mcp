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
internal sealed class GuidelineResources(
    IGuidelineRepository repository,
    IGuidelineAutomationMetadataProvider? automationMetadataProvider = null)
{
    private static string ServerName => "azure-pipelines-guidelines";
    private static string ServerVersion => "1.0.0";

    // Compact JSON with camel-case property names. Null values are omitted so AI clients
    // receive smaller responses and the shared contract stays predictable.
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    // ── adog://capabilities ──────────────────────────────────────────────────

    /// <summary>
    /// Returns the cacheable MCP surface and current catalogue version.
    /// </summary>
    [McpServerResource(
        UriTemplate = "adog://capabilities",
        Name = "capabilities",
        Title = "MCP capabilities",
        MimeType = "application/json")]
    [Description(
        "Returns the server version, catalogue version, supported transports, and the " +
        "currently available tools, resources, and prompts.")]
    internal Task<string> GetCapabilitiesAsync()
    {
        CapabilitiesResponseDto capabilities = new(
            ServerName,
            ServerVersion,
            repository.ContentVersion,
            ["stdio", "streamable-http"],
            [
                "analyze_template",
                "list_guidelines",
                "get_guideline",
                "search_guidelines",
                "list_categories"
            ],
            [
                "adog://capabilities",
                "adog://guidelines",
                "adog://guidelines/version",
                "adog://guidelines/category/{category}",
                "adog://guidelines/{id}",
                "adog://guidelines/{id}/automation"
            ],
            [
                "review",
                "review-category",
                "review-guideline",
                "explain-guideline",
                "find-guidelines",
                "list-guidelines",
                "list-categories"
            ],
            new CapabilitiesSupportDto(AutomationMetadata: true, Prompts: true));

        return Task.FromResult(JsonSerializer.Serialize(capabilities, _jsonOptions));
    }

    // ── adog://guidelines ─────────────────────────────────────────────────────

    /// <summary>
    /// Returns the full guideline catalogue as a JSON array.
    /// Each element contains id, title, category, and severity.
    /// </summary>
    [McpServerResource(
        UriTemplate = "adog://guidelines",
        Name = "guidelines",
        Title = "Guideline catalogue",
        MimeType = "application/json")]
    [Description(
        "The complete Azure Pipelines guideline catalogue. " +
        "Returns a JSON array; each element contains id, title, category, and severity.")]
    internal Task<string> GetAllGuidelinesAsync()
    {
        IReadOnlyList<GuidelineDefinition> all = repository.GetAll();

        GuidelineSummaryDto[] summaries = new GuidelineSummaryDto[all.Count];
        for (int i = 0; i < all.Count; i++)
        {
            GuidelineDefinition g = all[i];
            summaries[i] = new GuidelineSummaryDto(
                g.Id.Value,
                g.Title,
                EnumToJsonString(g.Category),
                EnumToJsonString(g.Severity));
        }

        return Task.FromResult(JsonSerializer.Serialize(summaries, _jsonOptions));
    }

    // ── adog://guidelines/version ───────────────────────────────────────────

    /// <summary>
    /// Returns a small cache fingerprint for the current guideline catalogue.
    /// </summary>
    [McpServerResource(
        UriTemplate = "adog://guidelines/version",
        Name = "guidelines-version",
        Title = "Guideline catalogue version",
        MimeType = "application/json")]
    [Description(
        "Returns a small JSON object with the current guideline catalogue version. " +
        "Clients can use this as a cache key and skip refetching the full catalogue when the version is unchanged.")]
    internal Task<string> GetCatalogueVersionAsync()
    {
        return Task.FromResult(JsonSerializer.Serialize(new CatalogueVersionResponseDto(repository.ContentVersion), _jsonOptions));
    }

    // ── adog://guidelines/category/{category} ──────────────────────────────

    /// <summary>
    /// Returns the guideline catalogue for a single category.
    /// </summary>
    [McpServerResource(
        UriTemplate = "adog://guidelines/category/{category}",
        Name = "guidelines-by-category",
        Title = "Guideline catalogue by category",
        MimeType = "application/json")]
    [Description(
        "Returns the guideline catalogue filtered to a single category. " +
        "Supply the category as the {category} path segment, for example adog://guidelines/category/steps. " +
        "Returns a JSON array with id, title, category, and severity for each matching guideline.")]
    internal Task<string> GetGuidelinesByCategoryAsync(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return Task.FromResult(JsonSerializer.Serialize(new ErrorResponseDto("Path segment 'category' is required."), _jsonOptions));
        }

        if (!TryParseCategory(category, out GuidelineCategory parsedCategory))
        {
            return Task.FromResult(JsonSerializer.Serialize(
                new ErrorResponseDto($"Unknown category '{category}'. Allowed values: general, jobs, parameters, pipelines, stages, steps, variables."),
                _jsonOptions));
        }

        IReadOnlyList<GuidelineDefinition> matching = repository.GetByCategory(parsedCategory);
        GuidelineSummaryDto[] summaries = new GuidelineSummaryDto[matching.Count];
        for (int i = 0; i < matching.Count; i++)
        {
            GuidelineDefinition guideline = matching[i];
            summaries[i] = new GuidelineSummaryDto(
                guideline.Id.Value,
                guideline.Title,
                EnumToJsonString(guideline.Category),
                EnumToJsonString(guideline.Severity));
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
        Title = "Guideline detail",
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
                JsonSerializer.Serialize(new ErrorResponseDto("Path segment 'id' is required."), _jsonOptions));
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
                    new ErrorResponseDto(
                        $"'{id}' is not a valid guideline ID. " +
                        "Expected format: ADOG-{CATEGORY}-{NNN}, e.g. ADOG-STEPS-001."),
                    _jsonOptions));
        }

        GuidelineDefinition? guideline = repository.FindById(guidelineId);

        if (guideline is null)
        {
            return Task.FromResult(
                JsonSerializer.Serialize(new ErrorResponseDto($"Guideline '{id}' not found."), _jsonOptions));
        }

        return Task.FromResult(JsonSerializer.Serialize(ToDetailDto(guideline), _jsonOptions));
    }

    // ── adog://guidelines/{id}/automation ─────────────────────────────────────

    /// <summary>
    /// Returns automation metadata for a single guideline identified by its stable ID.
    /// </summary>
    [McpServerResource(
        UriTemplate = "adog://guidelines/{id}/automation",
        Name = "guideline-automation",
        Title = "Guideline automation metadata",
        MimeType = "application/json")]
    [Description(
        "Returns the local automation status and reason for a single Azure Pipelines guideline. " +
        "Supply the stable guideline ID as the {id} path segment, e.g. adog://guidelines/ADOG-STEPS-001/automation.")]
    internal Task<string> GetGuidelineAutomationAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Task.FromResult(
                JsonSerializer.Serialize(new ErrorResponseDto("Path segment 'id' is required."), _jsonOptions));
        }

        GuidelineId guidelineId;
        try
        {
            guidelineId = new GuidelineId(id);
        }
        catch (ArgumentException)
        {
            return Task.FromResult(JsonSerializer.Serialize(
                new ErrorResponseDto(
                    $"'{id}' is not a valid guideline ID. " +
                    "Expected format: ADOG-{CATEGORY}-{NNN}, e.g. ADOG-STEPS-001."),
                _jsonOptions));
        }

        if (repository.FindById(guidelineId) is null)
        {
            return Task.FromResult(JsonSerializer.Serialize(
                new ErrorResponseDto($"Guideline '{id}' not found."), _jsonOptions));
        }

        GuidelineAutomationMetadata metadata = automationMetadataProvider?.GetAutomationMetadata(guidelineId)
            ?? new GuidelineAutomationMetadata(
                GuidelineAutomationStatus.NotAutomatable,
                "No local automation metadata is available.");

        return Task.FromResult(JsonSerializer.Serialize(
            new GuidelineAutomationMetadataDto(
                guidelineId.Value,
                EnumToJsonString(metadata.Status),
                metadata.Reason),
            _jsonOptions));
    }

    // ── Helpers ──────────────────────────────────────────────

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

    private GuidelineDetailDto ToDetailDto(GuidelineDefinition g)
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
            GetAutomationStatus(g),
            GetAutomationReason(g));
    }

    private string GetAutomationStatus(GuidelineDefinition guideline) =>
        EnumToJsonString(automationMetadataProvider?.GetAutomationMetadata(guideline.Id)?.Status ?? GuidelineAutomationStatus.NotAutomatable);

    private string GetAutomationReason(GuidelineDefinition guideline) =>
        automationMetadataProvider?.GetAutomationMetadata(guideline.Id)?.Reason ?? "No local automation metadata is available.";

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

    }
