using System.Text.Json;
using System.Text.Json.Serialization;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Mcp.Tools;

/// <summary>
/// Provides shared serialization and mapping helpers for guideline MCP tools.
/// </summary>
internal static class GuidelineToolSupport
{
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    internal static GuidelineSummaryDto CreateSummary(GuidelineDefinition guideline) =>
        new(
            guideline.Id.Value,
            guideline.Title,
            ToJsonString(guideline.Category),
            ToJsonString(guideline.Severity));

    internal static bool TryParseCategory(string value, out GuidelineCategory result)
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

    internal static GuidelineDetailDto ToDetailDto(
        GuidelineDefinition guideline,
        IGuidelineAutomationMetadataProvider? automationMetadataProvider)
    {
        DetectionHintDto[]? hints = guideline.DetectionHints.Count > 0
            ? BuildHintDtos(guideline.DetectionHints)
            : null;
        FixDto? fix = guideline.Fix is null
            ? null
            : new FixDto(guideline.Fix.Summary, guideline.Fix.Before, guideline.Fix.After);

        GuidelineAutomationMetadata? automationMetadata =
            automationMetadataProvider?.GetAutomationMetadata(guideline.Id);

        return new GuidelineDetailDto(
            guideline.Id.Value,
            guideline.Title,
            ToJsonString(guideline.Category),
            ToJsonString(guideline.Severity),
            guideline.Description,
            guideline.Rationale,
            guideline.Tags.Count > 0 ? [.. guideline.Tags] : null,
            hints,
            fix,
            guideline.References.Count > 0 ? [.. guideline.References] : null,
            ToJsonString(automationMetadata?.Status ?? GuidelineAutomationStatus.NotAutomatable),
            automationMetadata?.Reason ?? "No local automation metadata is available.");
    }

    internal static string ToJsonString<T>(T value) where T : struct, Enum
    {
        string name = value.ToString();
        return string.Create(name.Length, name, static (span, source) =>
        {
            for (int i = 0; i < source.Length; i++)
            {
                char c = source[i];
                span[i] = c is >= 'A' and <= 'Z' ? (char)(c + 32) : c;
            }
        });
    }

    private static DetectionHintDto[] BuildHintDtos(IReadOnlyList<DetectionHint> hints)
    {
        DetectionHintDto[] result = new DetectionHintDto[hints.Count];
        for (int i = 0; i < hints.Count; i++)
        {
            DetectionHint hint = hints[i];
            result[i] = new DetectionHintDto(
                ToJsonString(hint.Kind), ToJsonString(hint.Scope), hint.Expression, hint.Description);
        }
        return result;
    }
}
