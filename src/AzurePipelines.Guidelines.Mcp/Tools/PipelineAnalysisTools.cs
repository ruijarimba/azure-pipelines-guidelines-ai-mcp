using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AzurePipelines.Guidelines.Analysis;
using AzurePipelines.Guidelines.Core;
using ModelContextProtocol.Server;

namespace AzurePipelines.Guidelines.Mcp.Tools;

/// <summary>
/// MCP tool handlers for analysing Azure Pipelines YAML against the loaded guidelines.
/// </summary>
[McpServerToolType]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance", "CA1812:Avoid uninstantiated internal classes",
    Justification = "Instantiated by the MCP SDK via dependency injection.")]
internal sealed class PipelineAnalysisTools(
    IPipelineParser parser,
    IPipelineAnalyser analyser,
    PipelinePathResolver pathResolver,
    IGuidelineRepository repository)
{
    // Compact JSON with camel-case property names. Null values are omitted so AI clients
    // receive smaller responses and the shared contract stays predictable.
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    // ── analyze_pipeline ─────────────────────────────────────────────────────

    /// <summary>
    /// Analyses raw Azure Pipelines YAML content against the loaded guidelines
    /// and returns any violations found.
    /// </summary>
    [McpServerTool(Name = "analyze_pipeline")]
    [Description(
        "Analyses Azure Pipelines YAML content against the loaded guidelines and returns " +
        "compact JSON containing line-level diagnostics and deduplicated rule summaries. Each rule " +
        "summary includes guidance and reference links. When presenting results, render every " +
        "reference URL as a Markdown link. Use get_guideline only when full remediation details are needed. " +
        "Pass an optional category to restrict analysis to one guideline category, or an " +
        "optional comma-separated list of guideline IDs to restrict to specific rules.")]
    internal async Task<string> AnalyzePipelineAsync(
        [Description("The raw YAML content of the Azure Pipelines file to analyse.")]
        string yaml,
        [Description(
            "Optional comma-separated list of guideline IDs to check " +
            "(e.g. \"ADOG-STEPS-001,ADOG-JOBS-006\"). " +
            "Omit or pass null to run all rules.")]
        string? guidelineIds = null,
        [Description(
            "Optional category filter. " +
            "Allowed values: general, jobs, parameters, pipelines, stages, steps, variables. " +
            "Omit or pass null to include all categories.")]
        string? category = null,
        [Description("Output format: json (default) for diagnostics and rule summaries, or compact for findings only.")]
        string? format = "json",
        [Description("Include rule guidance in the response. Defaults to false; use get_guideline for full remediation details.")]
        bool includeGuidance = false)
    {
        if (string.IsNullOrWhiteSpace(yaml))
        {
            return JsonSerializer.Serialize(
                new ErrorResponse("Parameter 'yaml' is required."), _jsonOptions);
        }

        if (!TryParseOutputFormat(format, out _, out bool useCompact))
        {
            return JsonSerializer.Serialize(
                new ErrorResponse("Unknown format. Allowed values: json, compact."), _jsonOptions);
        }

        PipelineDocument document;
        try
        {
            document = parser.Parse(yaml, filePath: "(inline)");
        }
        catch (PipelineParsingException ex)
        {
            return JsonSerializer.Serialize(
                new ErrorResponse($"Failed to parse YAML: {ex.Message}"), _jsonOptions);
        }

        if (!TryBuildOptions(guidelineIds, category, out AnalysisOptions options, out string? optionsError))
        {
            return JsonSerializer.Serialize(new ErrorResponse(optionsError!), _jsonOptions);
        }

        AnalysisResult result = await analyser
            .AnalyseAsync(document, options)
            .ConfigureAwait(false);

        if (useCompact)
        {
            return JsonSerializer.Serialize(new CompactDiagnosticsResponseDto(
                BuildCompactDiagnosticDtos(result.Diagnostics)), _jsonOptions);
        }

        return JsonSerializer.Serialize(BuildAnalysisResponse(result.Diagnostics, includeGuidance), _jsonOptions);
    }

    // ── analyze_pipeline_paths ───────────────────────────────────────────────

    /// <summary>
    /// Analyses one or more Azure Pipelines YAML files or directories and returns any violations found.
    /// </summary>
    [McpServerTool(Name = "analyze_pipeline_paths")]
    [Description(
        "Analyses one or more Azure Pipelines YAML files or directories against the loaded guidelines " +
        "and returns per-file diagnostics plus compact deduplicated rule summaries with reference links. " +
        "Use format=markdown for a compact user-facing report with linked rule IDs; use the default " +
        "JSON format for structured processing. Directories are scanned recursively. " +
        "Rule guidance is omitted from JSON by default; set includeGuidance to true when needed. " +
        "Pass an optional category to restrict analysis to one guideline category, or an " +
        "optional comma-separated list of guideline IDs to restrict to specific rules.")]
    internal async Task<string> AnalyzePipelinePathsAsync(
        [Description("One or more file or directory paths to analyse. Directories are scanned recursively.")]
        string[] paths,
        [Description(
            "Optional comma-separated list of guideline IDs to check " +
            "(e.g. \"ADOG-STEPS-001,ADOG-JOBS-006\"). Omit or pass null to run all rules.")]
        string? guidelineIds = null,
        [Description(
            "Optional category filter. " +
            "Allowed values: general, jobs, parameters, pipelines, stages, steps, variables. " +
            "Omit or pass null to include all categories.")]
        string? category = null,
        [Description(
            "Output format: json (default) for structured diagnostics or markdown for a compact " +
            "user-facing report with linked rule IDs.")]
        string? format = "json",
        [Description("Include rule guidance in JSON responses. Defaults to false; Markdown always includes guidance.")]
        bool includeGuidance = false)
    {
        if (paths is null || paths.Length == 0 || paths.All(string.IsNullOrWhiteSpace))
        {
            return JsonSerializer.Serialize(
                new ErrorResponse("Parameter 'paths' is required."), _jsonOptions);
        }

        if (!TryParseOutputFormat(format, out bool useMarkdown, out bool useCompact))
        {
            return JsonSerializer.Serialize(
                new ErrorResponse("Unknown format. Allowed values: json, compact, markdown."), _jsonOptions);
        }

        IReadOnlyList<string> discoveredPaths;
        try
        {
            discoveredPaths = pathResolver.Resolve(paths);
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or ArgumentException)
        {
            return JsonSerializer.Serialize(new ErrorResponse(ex.Message), _jsonOptions);
        }

        if (!TryBuildOptions(guidelineIds, category, out AnalysisOptions options, out string? optionsError))
        {
            return JsonSerializer.Serialize(new ErrorResponse(optionsError!), _jsonOptions);
        }

        List<FileAnalysisResultDto> fileResults = [];
        List<CompactFileAnalysisResultDto> compactFileResults = [];
        List<Diagnostic> allDiagnostics = [];

        foreach (string discoveredPath in discoveredPaths)
        {
            string yaml;
            try
            {
                yaml = await File.ReadAllTextAsync(discoveredPath).ConfigureAwait(false);
            }
            catch (IOException ex)
            {
                return JsonSerializer.Serialize(
                    new ErrorResponse($"Cannot read file {discoveredPath}: {ex.Message}"), _jsonOptions);
            }

            PipelineDocument document;
            try
            {
                document = parser.Parse(yaml, discoveredPath);
            }
            catch (PipelineParsingException ex)
            {
                return JsonSerializer.Serialize(
                    new ErrorResponse($"Failed to parse YAML in {discoveredPath}: {ex.Message}"), _jsonOptions);
            }

            AnalysisResult result = await analyser
                .AnalyseAsync(document, options)
                .ConfigureAwait(false);

            if (useCompact)
            {
                compactFileResults.Add(new CompactFileAnalysisResultDto(
                    discoveredPath,
                    BuildCompactDiagnosticDtos(result.Diagnostics, discoveredPath)));
            }
            else
            {
                fileResults.Add(new FileAnalysisResultDto(discoveredPath, BuildDiagnosticDtos(result.Diagnostics)));
            }
            allDiagnostics.AddRange(result.Diagnostics);
        }

        RuleDetailDto[] rules = BuildRuleDetails(allDiagnostics, includeGuidance || useMarkdown);
        if (useMarkdown)
        {
            return BuildMarkdownReport(fileResults, allDiagnostics, rules);
        }

        if (useCompact)
        {
            return JsonSerializer.Serialize(new CompactPathsResponseDto([.. compactFileResults]), _jsonOptions);
        }

        return JsonSerializer.Serialize(new AnalysisPathsResponseDto([.. fileResults], rules), _jsonOptions);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool TryBuildOptions(
        string? guidelineIds,
        string? category,
        out AnalysisOptions options,
        out string? error)
    {
        error = null;

        IReadOnlyList<GuidelineCategory>? includedCategories = null;
        if (!string.IsNullOrWhiteSpace(category))
        {
            List<GuidelineCategory> parsedCategories = [];
            foreach (string part in category.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                if (!TryParseCategory(part, out GuidelineCategory parsedCategory))
                {
                    options = AnalysisOptions.Default;
                    error = $"Unknown category '{part}'. " +
                        "Allowed values: general, jobs, parameters, pipelines, stages, steps, variables.";
                    return false;
                }

                parsedCategories.Add(parsedCategory);
            }

            includedCategories = parsedCategories.Distinct().ToArray();
        }

        IReadOnlyList<GuidelineId>? includedIds = null;
        if (!string.IsNullOrWhiteSpace(guidelineIds))
        {
            List<GuidelineId> ids = [];
            foreach (string part in guidelineIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                try
                {
                    ids.Add(new GuidelineId(part));
                }
                catch (ArgumentException)
                {
                    // Keep valid filters when one interactive-client input is malformed.
                }
            }

            if (ids.Count > 0)
            {
                includedIds = ids;
            }
        }

        options = includedCategories is null && includedIds is null
            ? AnalysisOptions.Default
            : new AnalysisOptions(
                IncludedCategories: includedCategories,
                IncludedGuidelineIds: includedIds);

        return true;
    }

    /// <summary>Parses the requested analysis response format.</summary>
    /// <param name="format">The requested format name.</param>
    /// <param name="useMarkdown">Whether the response should be rendered as Markdown.</param>
    /// <param name="useCompact">Whether the response should use the compact findings-only shape.</param>
    /// <returns><see langword="true"/> when the format is supported.</returns>
    private static bool TryParseOutputFormat(string? format, out bool useMarkdown, out bool useCompact)
    {
        if (string.IsNullOrWhiteSpace(format) || string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
        {
            useMarkdown = false;
            useCompact = false;
            return true;
        }

        if (string.Equals(format, "markdown", StringComparison.OrdinalIgnoreCase))
        {
            useMarkdown = true;
            useCompact = false;
            return true;
        }

        if (string.Equals(format, "compact", StringComparison.OrdinalIgnoreCase))
        {
            useMarkdown = false;
            useCompact = true;
            return true;
        }

        useMarkdown = false;
        useCompact = false;
        return false;
    }

    /// <summary>Parses a category filter value.</summary>
    /// <param name="value">The category text.</param>
    /// <param name="result">The parsed category when successful.</param>
    /// <returns><see langword="true"/> when the category is supported.</returns>
    private static bool TryParseCategory(string value, out GuidelineCategory result)
    {
        result = value.ToUpperInvariant() switch
        {
            "GENERAL"    => GuidelineCategory.General,
            "JOBS"       => GuidelineCategory.Jobs,
            "PARAMETERS" => GuidelineCategory.Parameters,
            "PIPELINES"  => GuidelineCategory.Pipelines,
            "STAGES"     => GuidelineCategory.Stages,
            "STEPS"      => GuidelineCategory.Steps,
            "VARIABLES"  => GuidelineCategory.Variables,
            _            => (GuidelineCategory)(-1),
        };

        return (int)result >= 0;
    }

    /// <summary>Maps diagnostics to the compact MCP response contract.</summary>
    /// <param name="diagnostics">The diagnostics to map.</param>
    /// <returns>The mapped diagnostics.</returns>
    private DiagnosticDto[] BuildDiagnosticDtos(IReadOnlyList<Diagnostic> diagnostics)
    {
        DiagnosticDto[] dtos = new DiagnosticDto[diagnostics.Count];
        for (int i = 0; i < diagnostics.Count; i++)
        {
            Diagnostic d = diagnostics[i];
            GuidelineDefinition? guideline = repository.FindById(d.GuidelineId);
            dtos[i] = new DiagnosticDto(
                d.GuidelineId.Value,
                EnumToJsonString(d.Severity),
                guideline is null ? null : EnumToGuidanceString(guideline.Severity),
                d.Message,
                d.Line);
        }

        return dtos;
    }

    /// <summary>Maps diagnostics to the token-efficient compact response contract.</summary>
    /// <param name="diagnostics">The diagnostics to map.</param>
    /// <param name="filePath">The optional source file path.</param>
    /// <returns>The mapped compact diagnostics.</returns>
    private CompactDiagnosticDto[] BuildCompactDiagnosticDtos(
        IReadOnlyList<Diagnostic> diagnostics,
        string? filePath = null)
    {
        CompactDiagnosticDto[] dtos = new CompactDiagnosticDto[diagnostics.Count];
        for (int i = 0; i < diagnostics.Count; i++)
        {
            Diagnostic diagnostic = diagnostics[i];
            GuidelineDefinition? guideline = repository.FindById(diagnostic.GuidelineId);
            dtos[i] = new CompactDiagnosticDto(
                diagnostic.GuidelineId.Value,
                EnumToJsonString(diagnostic.Severity),
                guideline is null ? null : EnumToGuidanceString(guideline.Severity),
                diagnostic.Message,
                filePath,
                diagnostic.Line);
        }

        return dtos;
    }

    /// <summary>Builds the single-document analysis response.</summary>
    /// <param name="diagnostics">The diagnostics produced by analysis.</param>
    /// <param name="includeGuidance">Whether to include rule guidance in the response.</param>
    /// <returns>The structured analysis response.</returns>
    private AnalysisResponseDto BuildAnalysisResponse(
        IReadOnlyList<Diagnostic> diagnostics,
        bool includeGuidance) =>
        new(BuildDiagnosticDtos(diagnostics), BuildRuleDetails(diagnostics, includeGuidance));

    /// <summary>Builds one linked rule summary for each distinct violated guideline.</summary>
    /// <param name="diagnostics">The diagnostics whose guideline summaries are needed.</param>
    /// <param name="includeGuidance">Whether to include rule guidance in each summary.</param>
    /// <returns>Distinct rule details in first-seen order.</returns>
    private RuleDetailDto[] BuildRuleDetails(
        IEnumerable<Diagnostic> diagnostics,
        bool includeGuidance)
    {
        List<RuleDetailDto> details = [];
        HashSet<string> ruleIds = new(StringComparer.Ordinal);

        foreach (Diagnostic diagnostic in diagnostics)
        {
            if (!ruleIds.Add(diagnostic.GuidelineId.Value))
            {
                continue;
            }

            GuidelineDefinition? guideline = repository.FindById(diagnostic.GuidelineId);
            if (guideline is null)
            {
                continue;
            }

            details.Add(new RuleDetailDto(
                guideline.Id.Value,
                guideline.Title,
                EnumToGuidanceString(guideline.Severity),
                includeGuidance ? guideline.Fix?.Summary ?? guideline.Description : null,
                guideline.References.Count > 0 ? [.. guideline.References] : null));
        }

        return [.. details];
    }

    /// <summary>Renders multi-file analysis results as a compact Markdown report.</summary>
    /// <param name="files">The per-file analysis results.</param>
    /// <param name="diagnostics">All diagnostics across the analyzed files.</param>
    /// <param name="rules">Distinct rule details used for linked summaries.</param>
    /// <returns>The Markdown report.</returns>
    private static string BuildMarkdownReport(
        IReadOnlyList<FileAnalysisResultDto> files,
        List<Diagnostic> diagnostics,
        IReadOnlyList<RuleDetailDto> rules)
    {
        Dictionary<string, RuleDetailDto> rulesById = rules.ToDictionary(static rule => rule.Id, StringComparer.Ordinal);
        StringBuilder report = new();

        report.AppendLine("## Azure Pipelines Guideline Analysis");
        report.AppendLine();
        report.Append("Analyzed ").Append(files.Count).AppendLine(" YAML files.");
        report.AppendLine();
        report.AppendLine("### Severity counts");
        report.AppendLine();
        report.AppendLine("| Severity | Count |");
        report.AppendLine("| --- | ---: |");
        AppendSeverityCount(report, diagnostics, DiagnosticSeverity.Error, "Error");
        AppendSeverityCount(report, diagnostics, DiagnosticSeverity.Warning, "Warning");
        AppendSeverityCount(report, diagnostics, DiagnosticSeverity.Info, "Info");
        report.Append("| Total | ").Append(diagnostics.Count).AppendLine(" |");
        report.AppendLine();
        report.AppendLine("### Violated guidelines");
        report.AppendLine();
        report.AppendLine("| Rule | Title | Count | Advisory | Guidance |");
        report.AppendLine("| --- | --- | ---: | --- | --- |");

        foreach (IGrouping<string, Diagnostic> group in diagnostics
            .GroupBy(static diagnostic => diagnostic.GuidelineId.Value)
            .OrderByDescending(static group => group.Count())
            .ThenBy(static group => group.Key, StringComparer.Ordinal))
        {
            rulesById.TryGetValue(group.Key, out RuleDetailDto? rule);
            report.Append("| ")
                .Append(FormatRuleLink(group.Key, rule?.References))
                .Append(" | ")
                .Append(EscapeTableCell(rule?.Title ?? group.Key))
                .Append(" | ")
                .Append(group.Count())
                .Append(" | ")
                .Append(EscapeTableCell(rule?.Advisory ?? "Unknown"))
                .Append(" | ")
                .Append(EscapeTableCell(rule?.Guidance ?? "No additional guidance is available."))
                .AppendLine(" |");
        }

        report.AppendLine();
        report.AppendLine("### Files");
        report.AppendLine();
        report.AppendLine("| File | Errors | Warnings | Info |");
        report.AppendLine("| --- | ---: | ---: | ---: |");

        foreach (FileAnalysisResultDto file in files)
        {
            report.Append("| ")
                .Append(EscapeTableCell(file.FilePath))
                .Append(" | ")
                .Append(file.Diagnostics.Count(static diagnostic => diagnostic.Severity == "error"))
                .Append(" | ")
                .Append(file.Diagnostics.Count(static diagnostic => diagnostic.Severity == "warning"))
                .Append(" | ")
                .Append(file.Diagnostics.Count(static diagnostic => diagnostic.Severity == "info"))
                .AppendLine(" |");
        }

        return report.ToString();
    }

    /// <summary>Appends one severity row to the Markdown report.</summary>
    /// <param name="report">The report builder.</param>
    /// <param name="diagnostics">The diagnostics to count.</param>
    /// <param name="severity">The severity to count.</param>
    /// <param name="label">The display label.</param>
    private static void AppendSeverityCount(
        StringBuilder report,
        IReadOnlyList<Diagnostic> diagnostics,
        DiagnosticSeverity severity,
        string label) =>
        report.Append("| ")
            .Append(label)
            .Append(" | ")
            .Append(diagnostics.Count(diagnostic => diagnostic.Severity == severity))
            .AppendLine(" |");

    /// <summary>Formats a rule identifier as a Markdown link when an HTTP reference exists.</summary>
    /// <param name="ruleId">The guideline identifier.</param>
    /// <param name="references">Candidate guideline references.</param>
    /// <returns>The linked or plain rule identifier.</returns>
    private static string FormatRuleLink(string ruleId, string[]? references)
    {
        string? reference = references?.FirstOrDefault(IsHttpUrl);
        return reference is null ? ruleId : $"[{ruleId}]({reference})";
    }

    /// <summary>Determines whether a value is an absolute HTTP or HTTPS URL.</summary>
    /// <param name="value">The candidate URL.</param>
    /// <returns><see langword="true"/> when the value is an HTTP URL.</returns>
    private static bool IsHttpUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    /// <summary>Escapes content that would otherwise alter a Markdown table.</summary>
    /// <param name="value">The table cell value.</param>
    /// <returns>The escaped single-line cell value.</returns>
    private static string EscapeTableCell(string value) =>
        value.Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);

    /// <summary>Converts an enum value to lowercase ASCII for JSON output.</summary>
    /// <param name="value">The enum value to convert.</param>
    /// <returns>The lowercase enum name.</returns>
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

    /// <summary>Converts a guideline strength to its original advisory wording.</summary>
    /// <param name="value">The guideline strength.</param>
    /// <returns>The lower-case advisory label.</returns>
    private static string EnumToGuidanceString(GuidelineSeverity value) =>
        value switch
        {
            GuidelineSeverity.Do => "do",
            GuidelineSeverity.DoNot => "don't",
            GuidelineSeverity.Avoid => "avoid",
            GuidelineSeverity.Consider => "consider",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
        };

    // These records remain nested because they are private, tool-specific response contracts.

    /// <summary>Represents one diagnostic in an MCP response.</summary>
    private sealed record DiagnosticDto(
        [property: JsonPropertyName("ruleId")] string RuleId,
        [property: JsonPropertyName("severity")] string Severity,
        [property: JsonPropertyName("guidance")] string? Guidance,
        [property: JsonPropertyName("message")] string Message,
        [property: JsonPropertyName("line")] int? Line);

    /// <summary>Represents one diagnostic in a compact MCP response.</summary>
    private sealed record CompactDiagnosticDto(
        [property: JsonPropertyName("ruleId")] string RuleId,
        [property: JsonPropertyName("severity")] string Severity,
        [property: JsonPropertyName("guidance")] string? Guidance,
        [property: JsonPropertyName("message")] string Message,
        [property: JsonPropertyName("file")] string? File,
        [property: JsonPropertyName("line")] int? Line);

    /// <summary>Represents a single-document analysis response.</summary>
    private sealed record AnalysisResponseDto(
        [property: JsonPropertyName("diagnostics")] DiagnosticDto[] Diagnostics,
        [property: JsonPropertyName("rules")] RuleDetailDto[] Rules);

    /// <summary>Represents diagnostics for one analyzed file.</summary>
    private sealed record FileAnalysisResultDto(
        [property: JsonPropertyName("filePath")] string FilePath,
        [property: JsonPropertyName("diagnostics")] DiagnosticDto[] Diagnostics);

    /// <summary>Represents a multi-file analysis response.</summary>
    private sealed record AnalysisPathsResponseDto(
        [property: JsonPropertyName("files")] FileAnalysisResultDto[] Files,
        [property: JsonPropertyName("rules")] RuleDetailDto[] Rules);

    /// <summary>Represents a compact single-document analysis response.</summary>
    private sealed record CompactDiagnosticsResponseDto(
        [property: JsonPropertyName("findings")] CompactDiagnosticDto[] Findings);

    /// <summary>Represents a compact multi-file analysis response.</summary>
    private sealed record CompactPathsResponseDto(
        [property: JsonPropertyName("files")] CompactFileAnalysisResultDto[] Files);

    /// <summary>Represents compact diagnostics for one analyzed file.</summary>
    private sealed record CompactFileAnalysisResultDto(
        [property: JsonPropertyName("file")] string File,
        [property: JsonPropertyName("findings")] CompactDiagnosticDto[] Findings);

    /// <summary>Represents a linked summary for one violated guideline.</summary>
    private sealed record RuleDetailDto(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("advisory")] string Advisory,
        [property: JsonPropertyName("guidance")] string? Guidance,
        [property: JsonPropertyName("references")] string[]? References);

    /// <summary>Represents an MCP tool error response.</summary>
    private sealed record ErrorResponse(
        [property: JsonPropertyName("error")] string Error);
}
