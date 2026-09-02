namespace AzurePipelines.Guidelines.Core;

/// <summary>
/// A machine-readable hint that describes how a tool or analyser can detect a potential
/// violation of its parent <see cref="GuidelineDefinition"/>.
/// </summary>
/// <param name="Kind">The type of detection hint.</param>
/// <param name="Scope">The YAML element this hint targets.</param>
/// <param name="Expression">
/// The expression to evaluate (e.g., a regex pattern or a YAML path expression).
/// May be <see langword="null"/> for <see cref="DetectionKind.Heuristic"/> hints.
/// </param>
/// <param name="Description">
/// A human-readable description of what the hint detects.
/// </param>
public sealed record DetectionHint(
    DetectionKind Kind,
    PipelineScope Scope,
    string? Expression,
    string Description);
