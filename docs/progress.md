# Work in Progress

This file is the **session handoff note** for AI agents and human contributors. Update it before every commit so the next session starts with accurate context.

## Current snapshot

| Area | Status |
| --- | --- |
| Current focus | Human review of the completed MCP server before creating and merging the pull request |
| Recent wins | Completed a Copilot instruction-file consistency pass: fixed duplicate/malformed headings in code-style.instructions.md and maintainability.instructions.md, realigned AGENTS.md's principle summary and decision diagram with agent-behaviour.instructions.md's 12 principles, added a new MCP tool output-safety guardrail (agent-behaviour.instructions.md §6.1, csharp-patterns.instructions.md §4.3, testing.instructions.md security section, ADR-016). Added the `explain_diagnostic` MCP tool, which returns one guideline's full detail by ID with an optional echoed diagnostic context (message, filePath, line, column); added related DTOs, capability discovery, tests, and documentation. Added MCP prompts, cache-friendly catalogue access, guideline automation metadata, analysis summaries, and token-usage guidance. Completed the documentation consistency review, Docker build-stage test execution, and multi-architecture Docker Hub publication. |
| Next up | Retrofit `RelativeTemplatePathRule` and `ParameterMissingValuesRule` (and any other rules found on a full audit) to sanitize/truncate matched pipeline text before interpolating it into diagnostic messages, per ADR-016; then monitor the companion manifest for new `ADOG-*` rules |

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
| Analysis and host consolidation | Consolidated pipeline and template analysis behind the `analyze_template` MCP tool and kept the host as a thin transport boundary. |
| Integration validation | Added folder-based integration tests for the real analysis stack and validated both MCP launch profiles. |
| Documentation and maintainability | Updated user documentation, architecture guidance, Solution Explorer registration, host guidance, and repository guardrails. |
| Docker distribution | Selected the ASP.NET runtime image, added Docker build-stage test execution, and published the multi-architecture MCP image to Docker Hub. |
| Copilot instruction-file consistency pass | Fixed duplicate/malformed headings in code-style.instructions.md and maintainability.instructions.md; realigned AGENTS.md's principle summary and decision diagram with the canonical 12 principles in agent-behaviour.instructions.md; added a new MCP tool output-safety guardrail (agent-behaviour.instructions.md §6.1, csharp-patterns.instructions.md §4.3, testing.instructions.md security section, ADR-016 in docs/decisions.md). |

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

Nothing is currently in progress.

---

## Next up

1. **Retrofit rule diagnostics for MCP tool output safety (ADR-016).** Add a shared sanitizer
   (truncate + strip control characters) and apply it to `RelativeTemplatePathRule` and
   `ParameterMissingValuesRule`, which currently interpolate unbounded matched pipeline text
   into diagnostic messages. Audit the remaining rules for the same pattern before considering
   this complete. Add the adversarial tests described in testing.instructions.md.
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
