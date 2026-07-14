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

    private static readonly JsonSerializerOptions _jsonOptions = new()
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
            .GetFromJsonAsync<ManifestDto>(_manifestUrl, _jsonOptions, cancellationToken)
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

    /// <summary>
    /// Converts manifest detection entries into the domain hint model, ignoring entries that
    /// cannot be interpreted safely.
    /// </summary>
    /// <param name="detectionDtos">The manifest detection entries, when present.</param>
    /// <returns>The valid detection hints in manifest order.</returns>
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

    /// <summary>
    /// Converts manifest fix guidance into the domain model when a summary is available.
    /// </summary>
    /// <param name="dto">The manifest fix entry, or <see langword="null"/>.</param>
    /// <returns>The mapped fix guidance, or <see langword="null"/> when no summary exists.</returns>
    private static FixGuidance? MapFix(FixDto? dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.Summary))
        {
            return null;
        }

        return new FixGuidance(dto.Summary, Before: null, After: null);
    }

    /// <summary>Parses a manifest category into its domain enum value.</summary>
    /// <param name="value">The manifest category text.</param>
    /// <param name="result">The parsed category when successful.</param>
    /// <returns><see langword="true"/> when the value is supported.</returns>
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

    /// <summary>Parses a manifest severity into its domain enum value.</summary>
    /// <param name="value">The manifest severity text.</param>
    /// <param name="result">The parsed severity when successful.</param>
    /// <returns><see langword="true"/> when the value is supported.</returns>
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

    /// <summary>Parses a manifest detection kind into its domain enum value.</summary>
    /// <param name="value">The manifest detection kind text.</param>
    /// <param name="result">The parsed detection kind when successful.</param>
    /// <returns><see langword="true"/> when the value is supported.</returns>
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

    /// <summary>Parses a manifest scope, defaulting unknown scopes to the general scope.</summary>
    /// <param name="value">The manifest scope text.</param>
    /// <param name="result">The parsed scope.</param>
    /// <returns>Always <see langword="true"/> because unknown scopes use a safe default.</returns>
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

    // These DTOs stay nested because they are private implementation details of this loader and
    // are instantiated by System.Text.Json via reflection.

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance", "CA1812:Avoid uninstantiated internal classes",
        Justification = "Instantiated by System.Text.Json deserialiser.")]
    private sealed class ManifestDto
    {
        /// <summary>Gets the guideline entries in the manifest.</summary>
        [JsonPropertyName("guidelines")]
        public List<GuidelineItemDto> Guidelines { get; init; } = [];
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance", "CA1812:Avoid uninstantiated internal classes",
        Justification = "Instantiated by System.Text.Json deserialiser.")]
    private sealed class GuidelineItemDto
    {
        /// <summary>Gets the stable guideline identifier.</summary>
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        /// <summary>Gets the manifest category.</summary>
        [JsonPropertyName("category")]
        public string? Category { get; init; }

        /// <summary>Gets the manifest severity.</summary>
        [JsonPropertyName("severity")]
        public string? Severity { get; init; }

        /// <summary>Gets the guideline title.</summary>
        [JsonPropertyName("title")]
        public string? Title { get; init; }

        /// <summary>Gets the guideline summary.</summary>
        [JsonPropertyName("summary")]
        public string? Summary { get; init; }

        /// <summary>Gets the primary manifest URL, when supplied.</summary>
        [JsonPropertyName("url")]
        public string? Url { get; init; }

        /// <summary>Gets the scopes to which the guideline applies.</summary>
        [JsonPropertyName("appliesTo")]
        public List<string>? AppliesTo { get; init; }

        /// <summary>Gets the manifest tags.</summary>
        [JsonPropertyName("tags")]
        public List<string>? Tags { get; init; }

        /// <summary>Gets supplemental related URLs.</summary>
        [JsonPropertyName("related")]
        public List<string>? Related { get; init; }

        /// <summary>Gets machine-readable detection entries.</summary>
        [JsonPropertyName("detection")]
        public List<DetectionItemDto>? Detection { get; init; }

        /// <summary>Gets the optional fix guidance.</summary>
        [JsonPropertyName("fix")]
        public FixDto? Fix { get; init; }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance", "CA1812:Avoid uninstantiated internal classes",
        Justification = "Instantiated by System.Text.Json deserialiser.")]
    private sealed class DetectionItemDto
    {
        /// <summary>Gets the detection kind.</summary>
        [JsonPropertyName("kind")]
        public string? Kind { get; init; }

        /// <summary>Gets the detection expression.</summary>
        [JsonPropertyName("pattern")]
        public string? Pattern { get; init; }

        /// <summary>Gets the scopes targeted by the detection entry.</summary>
        [JsonPropertyName("appliesTo")]
        public List<string>? AppliesTo { get; init; }

        /// <summary>Gets the human-readable detection message.</summary>
        [JsonPropertyName("message")]
        public string? Message { get; init; }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance", "CA1812:Avoid uninstantiated internal classes",
        Justification = "Instantiated by System.Text.Json deserialiser.")]
    private sealed class FixDto
    {
        /// <summary>Gets the fix summary.</summary>
        [JsonPropertyName("summary")]
        public string? Summary { get; init; }
    }
}
