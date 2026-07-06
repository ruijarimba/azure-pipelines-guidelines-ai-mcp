using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AzurePipelines.Guidelines.Core;

/// <summary>
/// Loads guideline definitions by fetching the JSON manifest from a URL
/// (typically the raw GitHub URL of the companion guidelines repository).
/// </summary>
public sealed class HttpGuidelineLoader : IGuidelineLoader
{
    /// <summary>
    /// The raw GitHub URL of the official guidelines manifest.
    /// </summary>
    public static readonly Uri DefaultManifestUrl = new(
        "https://raw.githubusercontent.com/ruijarimba/azure-pipelines-guidelines/main/data/guidelines.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;
    private readonly Uri _manifestUrl;

    /// <summary>
    /// Initialises a new <see cref="HttpGuidelineLoader"/> using the given
    /// <paramref name="httpClient"/> and an optional <paramref name="manifestUrl"/>.
    /// </summary>
    /// <param name="httpClient">
    /// The <see cref="HttpClient"/> to use for the request. Must not be
    /// <see langword="null"/>.
    /// </param>
    /// <param name="manifestUrl">
    /// The URL to fetch the manifest from. When <see langword="null"/>,
    /// <see cref="DefaultManifestUrl"/> is used.
    /// </param>
    public HttpGuidelineLoader(HttpClient httpClient, Uri? manifestUrl = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        _httpClient = httpClient;
        _manifestUrl = manifestUrl ?? DefaultManifestUrl;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<GuidelineDefinition>> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        ManifestDto? manifest = await _httpClient
            .GetFromJsonAsync<ManifestDto>(_manifestUrl, JsonOptions, cancellationToken)
            .ConfigureAwait(false);

        if (manifest?.Guidelines is null)
        {
            return [];
        }

        List<GuidelineDefinition> results = new(manifest.Guidelines.Count);

        foreach (GuidelineItemDto item in manifest.Guidelines)
        {
            GuidelineDefinition? guideline = MapToDefinition(item);

            if (guideline is not null)
            {
                results.Add(guideline);
            }
        }

        return results;
    }

    private static GuidelineDefinition? MapToDefinition(GuidelineItemDto item)
    {
        if (string.IsNullOrWhiteSpace(item.Id) ||
            string.IsNullOrWhiteSpace(item.Category) ||
            string.IsNullOrWhiteSpace(item.Severity) ||
            string.IsNullOrWhiteSpace(item.Title))
        {
            return null;
        }

        if (!TryParseCategory(item.Category, out GuidelineCategory category) ||
            !TryParseSeverity(item.Severity, out GuidelineSeverity severity))
        {
            return null;
        }

        GuidelineId id;
        try
        {
            id = new GuidelineId(item.Id);
        }
        catch (ArgumentException)
        {
            return null;
        }

        IReadOnlyList<DetectionHint> hints = MapDetectionHints(item.Detection);
        FixGuidance? fix = MapFix(item.Fix);

        IReadOnlyList<string> references = item.Related is { Count: > 0 }
            ? item.Related
            : [];

        return new GuidelineDefinition(
            id,
            category,
            severity,
            item.Title,
            Description: item.Summary ?? string.Empty,
            Rationale: null,
            Tags: item.Tags ?? [],
            DetectionHints: hints,
            Fix: fix,
            References: references);
    }

    private static List<DetectionHint> MapDetectionHints(
        IReadOnlyList<DetectionItemDto>? detectionDtos)
    {
        if (detectionDtos is null or { Count: 0 })
        {
            return [];
        }

        List<DetectionHint> hints = new(detectionDtos.Count);

        foreach (DetectionItemDto dto in detectionDtos)
        {
            if (string.IsNullOrWhiteSpace(dto.Kind))
            {
                continue;
            }

            if (!TryParseDetectionKind(dto.Kind, out DetectionKind kind))
            {
                continue;
            }

            // Use the first appliesTo scope, or General when absent.
            PipelineScope scope = PipelineScope.General;
            if (dto.AppliesTo is { Count: > 0 })
            {
                _ = TryParseScope(dto.AppliesTo[0], out scope);
            }

            hints.Add(new DetectionHint(
                kind,
                scope,
                Expression: dto.Pattern,
                Description: dto.Message ?? string.Empty));
        }

        return hints;
    }

    private static FixGuidance? MapFix(FixDto? dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.Summary))
        {
            return null;
        }

        return new FixGuidance(dto.Summary, Before: null, After: null);
    }

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

    private static bool TryParseSeverity(string value, out GuidelineSeverity result)
    {
        result = value.ToUpperInvariant() switch
        {
            "DO" => GuidelineSeverity.Do,
            "DO-NOT" => GuidelineSeverity.DoNot,
            "AVOID" => GuidelineSeverity.Avoid,
            "CONSIDER" => GuidelineSeverity.Consider,
            _ => (GuidelineSeverity)(-1),
        };

        return (int)result >= 0;
    }

    private static bool TryParseDetectionKind(string value, out DetectionKind result)
    {
        result = value.ToUpperInvariant() switch
        {
            "REGEX" => DetectionKind.Regex,
            "YAML-PATH" or "YAMLPATH" => DetectionKind.YamlPath,
            "HEURISTIC" => DetectionKind.Heuristic,
            _ => (DetectionKind)(-1),
        };

        return (int)result >= 0;
    }

    private static bool TryParseScope(string value, out PipelineScope result)
    {
        result = value.ToUpperInvariant() switch
        {
            "PIPELINE" => PipelineScope.Pipeline,
            "STAGE" or "STAGES" => PipelineScope.Stage,
            "JOB" or "JOBS" => PipelineScope.Job,
            "STEP" or "STEPS" => PipelineScope.Step,
            "TASK" => PipelineScope.Task,
            "VARIABLES" or "VARIABLE" => PipelineScope.Variables,
            "PARAMETERS" or "PARAMETER" => PipelineScope.Parameters,
            "TEMPLATE" => PipelineScope.Template,
            "GENERAL" => PipelineScope.General,
            _ => PipelineScope.General,
        };

        return true;
    }

    // ── Internal DTOs ─────────────────────────────────────────────────────────
    // These classes are instantiated by System.Text.Json via reflection.

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance", "CA1812:Avoid uninstantiated internal classes",
        Justification = "Instantiated by System.Text.Json deserialiser.")]
    private sealed class ManifestDto
    {
        [JsonPropertyName("guidelines")]
        public List<GuidelineItemDto> Guidelines { get; init; } = [];
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance", "CA1812:Avoid uninstantiated internal classes",
        Justification = "Instantiated by System.Text.Json deserialiser.")]
    private sealed class GuidelineItemDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("category")]
        public string? Category { get; init; }

        [JsonPropertyName("severity")]
        public string? Severity { get; init; }

        [JsonPropertyName("title")]
        public string? Title { get; init; }

        [JsonPropertyName("summary")]
        public string? Summary { get; init; }

        [JsonPropertyName("url")]
        public string? Url { get; init; }

        [JsonPropertyName("appliesTo")]
        public List<string>? AppliesTo { get; init; }

        [JsonPropertyName("tags")]
        public List<string>? Tags { get; init; }

        [JsonPropertyName("related")]
        public List<string>? Related { get; init; }

        [JsonPropertyName("detection")]
        public List<DetectionItemDto>? Detection { get; init; }

        [JsonPropertyName("fix")]
        public FixDto? Fix { get; init; }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance", "CA1812:Avoid uninstantiated internal classes",
        Justification = "Instantiated by System.Text.Json deserialiser.")]
    private sealed class DetectionItemDto
    {
        [JsonPropertyName("kind")]
        public string? Kind { get; init; }

        [JsonPropertyName("pattern")]
        public string? Pattern { get; init; }

        [JsonPropertyName("appliesTo")]
        public List<string>? AppliesTo { get; init; }

        [JsonPropertyName("message")]
        public string? Message { get; init; }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance", "CA1812:Avoid uninstantiated internal classes",
        Justification = "Instantiated by System.Text.Json deserialiser.")]
    private sealed class FixDto
    {
        [JsonPropertyName("summary")]
        public string? Summary { get; init; }
    }
}
