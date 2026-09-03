# Work in Progress

This file is the **session handoff note** for AI agents and human contributors. Update it before every commit so the next session starts with accurate context.

## Current snapshot

| Area | Status |
| --- | --- |
| Current focus | Maintaining MCP capability metadata and monitoring the companion manifest for new rules |
| Recent wins | Improved MCP discoverability with descriptive server initialization metadata, Azure Pipelines YAML-focused titles and descriptions for tools, resources, and prompts, and structured descriptors in `adog://capabilities`. Added regression coverage and updated the MCP reference and maintainer guidance. Previously completed the MCP output-safety retrofit, `explain_diagnostic` tool, prompts, cache-friendly catalogue access, guideline automation metadata, analysis summaries, token-usage guidance, documentation consistency review, Docker build-stage test execution, and multi-architecture Docker Hub publication. |
| Next up | Monitor the companion manifest for new `ADOG-*` rules and preserve MCP output-safety and discovery-metadata coverage when adding capabilities |

---

## How to update this file

Before committing, edit the sections below:

1. Move the current "In progress" item(s) to "Recently completed" once the work is complete.
2. Write what you were actively doing when the session ended under "In progress".
3. Revise "Next up" to reflect the true priority order.
4. Record anything unresolved under "Open questions".

---

## Recently completed

| Milestone | Summary |
| --- | --- |
| MCP diagnostic explanation | Added the `explain_diagnostic` MCP tool, related DTOs, capability discovery, tests, and documentation. |
| MCP capability expansion | Added prompts, cache-friendly catalogue access, summary-first guideline lookup, automation metadata, analysis summaries, and token-usage guidance. |
| Analysis and host consolidation | Consolidated pipeline and template analysis behind the `analyze_template_or_folder` MCP tool and kept the host as a thin transport boundary. |
| Integration validation | Added folder-based integration tests for the real analysis stack and validated both MCP launch profiles. |
| Documentation and maintainability | Updated user documentation, architecture guidance, Solution Explorer registration, host guidance, and repository guardrails. |
| Docker distribution | Selected the ASP.NET runtime image, added Docker build-stage test execution, and published the multi-architecture MCP image to Docker Hub. |
| Copilot instruction-file consistency pass | Fixed duplicate/malformed headings in code-style.instructions.md and maintainability.instructions.md; realigned AGENTS.md's principle summary and decision diagram with the canonical 12 principles in agent-behaviour.instructions.md; added a new MCP tool output-safety guardrail (agent-behaviour.instructions.md §6.1, csharp-patterns.instructions.md §4.3, testing.instructions.md security section, ADR-016 in docs/decisions.md). |
| MCP diagnostic output sanitization | Added `RuleHelpers.SanitizeForDiagnostic` and applied it to pipeline-derived diagnostic values; added adversarial sanitizer and rule regression tests. |
| MCP boundary output sanitization | Sanitized analysis diagnostics, parser/file errors, and echoed diagnostic context before returning MCP responses; added adversarial MCP regression tests. |
| MCP discovery metadata | Added descriptive server, tool, resource, and prompt metadata for Azure Pipelines YAML guideline discovery; added structured capability descriptors, tests, documentation, and maintainer guidance. |

---

## Implemented rules

All current `ADOG-*` guideline IDs from the companion manifest are already implemented in this repository.

| Rule ID | Class |
| --- | --- |
| `ADOG-GENERAL-001` | `RelativeTemplatePathRule` |
| `ADOG-GENERAL-002` | `StringEncodedConstructsRule` |
| `ADOG-GENERAL-003` | `ParameterSchemaAlignmentRule` |
| `ADOG-GENERAL-004` | `PipelineDocumentationRule` |
| `ADOG-GENERAL-005` | `FolderStructureRule` |
| `ADOG-GENERAL-006` | `InlineTemplateLogicRule` |
| `ADOG-GENERAL-007` | `HardCodedValuesRule` |
| `ADOG-JOBS-001` | `JobMissingCheckoutRule` |
| `ADOG-JOBS-002` | `MultipleStepsTemplatesInJobRule` |
| `ADOG-JOBS-003` | `JobLevelVariableRule` |
| `ADOG-JOBS-004` | `ValidationModeJobParameterRule` |
| `ADOG-JOBS-005` | `ReusableJobTemplateParametersRule` |
| `ADOG-JOBS-006` | `JobMissingTimeoutRule` |
| `ADOG-JOBS-007` | `EnvironmentParameterMinimizationRule` |
| `ADOG-JOBS-008` | `SingleResponsibilityJobRule` |
| `ADOG-PARAMETERS-001` | `ParameterGroupingRule` |
| `ADOG-PARAMETERS-002` | `ParameterMissingValuesRule` |
| `ADOG-PIPELINES-001` | `PipelineValidationModeRule` |
| `ADOG-STAGES-001` | `UseStagesForRelatedJobsRule` |
| `ADOG-STAGES-002` | `RunIndependentStagesInParallelRule` |
| `ADOG-STEPS-001` | `MacroSyntaxInStepsRule` |
| `ADOG-STEPS-002` | `TaskEnvironmentVariablesRule` |
| `ADOG-STEPS-003` | `DiagnosticLoggingRule` |
| `ADOG-STEPS-004` | `DiagnosticLoggingConsiderationRule` |
| `ADOG-STEPS-005` | `StepRetryRule` |
| `ADOG-STEPS-006` | `StepMissingTimeoutRule` |
| `ADOG-STEPS-007` | `StepTemplateParametersRule` |
| `ADOG-STEPS-008` | `ServiceConnectionAuthRule` |
| `ADOG-STEPS-009` | `StepParameterValidationRule` |
| `ADOG-STEPS-010` | `LargeExpressionInStepsRule` |
| `ADOG-STEPS-011` | `AzureKeyVaultTaskRule` |
| `ADOG-VARIABLES-001` | `ReadonlyVariableRule` |
| `ADOG-VARIABLES-002` | `VariableTemplateOrganizationRule` |
| `ADOG-VARIABLES-003` | `SecretLikeVariableRule` |
| `ADOG-VARIABLES-004` | `SeparateConfigurationRule` |
| `ADOG-VARIABLES-005` | `VariableScopeRule` |
| `ADOG-VARIABLES-006` | `MultiEnvironmentVariableTemplateRule` |

New rule template: follow `.github/prompts/implement-rule.prompt.md`.

---

## In progress

- No implementation work is currently in progress.

---

## Next up

1. **Monitor MCP output safety.** Preserve the shared diagnostic sanitizers and adversarial
   coverage when future rules or MCP handlers begin returning pipeline-derived text.
2. **Monitor the companion manifest for new `ADOG-*` rules** and add any new ones with the rule
   template workflow when they appear.

---

## Open questions / blockers

- The MCP tool output-safety retrofit (ADR-016) is scoped to rule diagnostics found during a
  sample review; a full audit of all rules is still needed to confirm the complete list of
  files that interpolate matched pipeline text.
- The next MCP capability should be selected from the ordered backlog above at the start of the
  next session.
- NuGet publication is out of scope. Package metadata and local packing remain in the project files.

