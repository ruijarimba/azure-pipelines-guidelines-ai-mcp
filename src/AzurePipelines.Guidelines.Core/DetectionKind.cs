namespace AzurePipelines.Guidelines.Core;

/// <summary>Kind of detection hint in a guideline's machine-readable metadata.</summary>
public enum DetectionKind
{
    /// <summary>Match against raw YAML text using a regular expression.</summary>
    Regex,

    /// <summary>A path or key condition in parsed YAML.</summary>
    YamlPath,

    /// <summary>Natural-language rule best evaluated by an LLM or custom check.</summary>
    Heuristic,
}
