namespace AzurePipelines.Guidelines.Core;

/// <summary>
/// Describes how reliably a guideline can be evaluated from a parsed Azure Pipelines YAML document.
/// </summary>
public enum GuidelineAutomationStatus
{
    /// <summary>YAML evidence can prove the rule's condition reliably.</summary>
    Enforceable,

    /// <summary>YAML evidence can suggest a concern but cannot prove a violation.</summary>
    Heuristic,

    /// <summary>The guideline needs context that is not available in a YAML document.</summary>
    NotAutomatable,
}
