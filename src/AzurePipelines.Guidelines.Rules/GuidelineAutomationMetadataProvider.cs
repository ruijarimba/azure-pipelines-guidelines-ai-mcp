using System.Collections.Frozen;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules;

/// <summary>
/// Resolves local automation capability metadata for implemented guideline rules.
/// </summary>
internal sealed class GuidelineAutomationMetadataProvider : IGuidelineAutomationMetadataProvider
{
    private static readonly FrozenDictionary<string, GuidelineAutomationMetadata> _metadata =
        new Dictionary<string, GuidelineAutomationMetadata>(StringComparer.Ordinal)
        {
            ["ADOG-GENERAL-001"] = Enforceable("Template references can be checked for absolute paths directly in local YAML."),
            ["ADOG-GENERAL-002"] = Heuristic("Quoted values can be valid YAML and valid task input."),
            ["ADOG-GENERAL-003"] = Heuristic("Parameter names do not prove their intended schema field."),
            ["ADOG-GENERAL-004"] = Enforceable("A first non-empty comment is a deterministic local documentation policy."),
            ["ADOG-GENERAL-005"] = Heuristic("Shared template roots are repository conventions."),
            ["ADOG-GENERAL-006"] = NotAutomatable("YAML alone cannot establish whether inline logic should be reused."),
            ["ADOG-GENERAL-007"] = Heuristic("Literal values can be intentional stable defaults."),
            ["ADOG-JOBS-001"] = Enforceable("A job's explicit checkout entries are directly identifiable in local YAML."),
            ["ADOG-JOBS-002"] = Enforceable("All non-checkout job steps can be counted as logic steps in local YAML."),
            ["ADOG-JOBS-003"] = Enforceable("Pipeline-root variable declarations are directly identifiable when jobs are present in local YAML."),
            ["ADOG-JOBS-004"] = NotAutomatable("Only some jobs need a non-destructive validation mode."),
            ["ADOG-JOBS-005"] = Heuristic("YAML cannot prove that a job template is reused or needs each control."),
            ["ADOG-JOBS-006"] = Enforceable("The parsed job node deterministically exposes timeoutInMinutes."),
            ["ADOG-JOBS-007"] = NotAutomatable("Environment parameter necessity depends on template consumers and policy."),
            ["ADOG-JOBS-008"] = NotAutomatable("Keyword matches cannot establish a job's responsibilities."),
            ["ADOG-PARAMETERS-001"] = NotAutomatable("Parameter grouping is an interface-design decision."),
            ["ADOG-PARAMETERS-002"] = NotAutomatable("YAML cannot determine whether a string has a finite valid value set."),
            ["ADOG-PIPELINES-001"] = NotAutomatable("Only deployment-capable pipelines need a validation mode."),
            ["ADOG-STAGES-001"] = NotAutomatable("YAML cannot establish which top-level jobs are related."),
            ["ADOG-STAGES-002"] = NotAutomatable("YAML cannot establish whether stages are independent."),
            ["ADOG-STEPS-001"] = Enforceable("Macro syntax is directly identifiable in YAML."),
            ["ADOG-STEPS-002"] = Heuristic("Variable use does not prove a step-level environment mapping is appropriate."),
            ["ADOG-STEPS-003"] = Heuristic("Logging quality cannot be proven from selected command text."),
            ["ADOG-STEPS-004"] = Heuristic("Logging sufficiency is contextual and task-specific."),
            ["ADOG-STEPS-005"] = NotAutomatable("Retry suitability depends on operation idempotency and failure modes."),
            ["ADOG-STEPS-006"] = Heuristic("Task-level timeouts are not appropriate for every task."),
            ["ADOG-STEPS-007"] = Enforceable("Supported step control settings and template parameters are directly identifiable in local YAML."),
            ["ADOG-STEPS-008"] = NotAutomatable("YAML cannot prove a service-connection-capable task alternative exists or applies."),
            ["ADOG-STEPS-009"] = NotAutomatable("Parameter validation needs depend on the template's accepted input domain."),
            ["ADOG-STEPS-010"] = Enforceable("Pipeline macro and template expressions are directly identifiable in local YAML step content."),
            ["ADOG-STEPS-011"] = Enforceable("AzureKeyVault task references are directly identifiable in YAML."),
            ["ADOG-VARIABLES-001"] = Heuristic("YAML cannot determine whether a variable should remain mutable."),
            ["ADOG-VARIABLES-002"] = NotAutomatable("Folder organization requires repository path context."),
            ["ADOG-VARIABLES-003"] = Heuristic("A secret-like name is not proof that an inline value is a secret."),
            ["ADOG-VARIABLES-004"] = NotAutomatable("Configuration ownership requires template and environment context."),
            ["ADOG-VARIABLES-005"] = Enforceable("Pipeline- and stage-scope variable declarations are directly identifiable in local YAML."),
            ["ADOG-VARIABLES-006"] = Heuristic("Environment naming and file purpose cannot be proven from one YAML file."),
        }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <inheritdoc/>
    public GuidelineAutomationMetadata? GetAutomationMetadata(GuidelineId guidelineId) =>
        _metadata.GetValueOrDefault(guidelineId.Value);

    private static GuidelineAutomationMetadata Enforceable(string reason) =>
        new(GuidelineAutomationStatus.Enforceable, reason);

    private static GuidelineAutomationMetadata Heuristic(string reason) =>
        new(GuidelineAutomationStatus.Heuristic, reason);

    private static GuidelineAutomationMetadata NotAutomatable(string reason) =>
        new(GuidelineAutomationStatus.NotAutomatable, reason);
}
