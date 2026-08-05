# Work in Progress

This file is the **session handoff note** for AI agents and human contributors.
Update it before every commit so the next session starts with accurate context.

## Current snapshot

| Area | Status |
| --- | --- |
| Current focus | The Docker Hub MCP image is published and documented; the repository is ready for the next MCP capability |
| Recent wins | Published the multi-architecture Docker Hub image, added fail-fast publishing checks, fixed the ASP.NET runtime dependency, and documented the one-image runtime decision |
| Next up | Choose the next MCP capability: concise prompts, server-side analysis summaries, or the dedicated token-usage guide |

---

## How to update this file

Before committing, edit the sections below:

1. Move the current "In progress" item(s) to "Recently completed" (with the commit hash once known).
2. Write what you were actively doing when the session ended under "In progress".
3. Revise "Next up" to reflect the true priority order.
4. Record anything unresolved under "Open questions".

---

## Recently completed

| Commit | Summary |
| --- | --- |
| `local` | docs: explain why the Docker image uses the ASP.NET runtime — one image supports stdio and HTTP; using the base runtime would require separately built, tested, published, and documented images |
| `383d7a7` | fix: use the ASP.NET runtime required by the MCP host; harden Docker Hub publishing checks; document Docker Hub and VS Code MCP setup; publish corrected multi-architecture `latest` image |
| `1c5aa2e` | feat: expose guideline automation metadata through MCP tools and resources |
| `local` | feat: add cache-friendly MCP guideline resources and summary-first guideline lookup for lower token use |
| `local` | feat: upgrade ModelContextProtocol/ModelContextProtocol.AspNetCore to stable 2.0.0; bump Microsoft.Extensions.* to 10.0.10; keep legacy SSE (Stateless=false, suppressed obsolete MCP9004) alongside default Streamable HTTP; add Title/ReadOnly tool hints; document the compatibility decision in ADR-015 |
| `local` | docs/comments: add inline comments, host README, and AGENTS updates to the MCP project so contributors without deep .NET knowledge can follow transport modes, launch profiles, and startup choices |
| `local` | feat: add optional SSE debug transport to MCP host so it can run under Visual Studio while VS Code connects over HTTP |
| `local` | feat: add source-mode and local Docker MCP launch scripts with local-client configuration guidance |
| `local` | feat: add config-file support for CLI defaults for `analyze` and `rules list` with CLI > environment > config > built-in default precedence |
| `3a8eb87` | test: add folder-based integration tests for the real analysis stack and validate the full solution quality gate |
| `8c3b576` | chore: mirror filesystem structure in Solution Explorer — rule 8 in agent-behaviour, slnx hierarchy fixes |
| `572af54` | chore: add non-code files to Solution Explorer (AGENTS.md per project, docs, .github) |
| `eec4316` | docs: expand CLI AGENTS.md with full option/example/format documentation |
| `699f2e4` | docs: add `docs/progress.md` session handoff log and link from `AGENTS.md` and `copilot-instructions` |
| `d72bf64` | feat: add `--category` filter to `adog analyze` and `--severity` filter to `adog rules list` — CLI and MCP |
| `e874e24` | docs: improve MCP tools documentation and fix accuracy issues |
| `428e088` | feat: support multi-file and directory analysis in CLI and MCP |
| `9a037da` | feat: add `adog-mcp` .NET global tool and Docker Hub image distribution |
| `bf07410` | chore: fix CLI packaging and harden CI pack workflow |
| `438da52` | test: raise unit test coverage above 90 % for all assemblies |
| `c71efa0` | feat: add IOutputFormatter, OutputFormatterFactory, exit code refactor, and console/compact formatters with tests |
| `1d0ac9a` | feat: add JSON analysis formatter and tests with camelCase output |
| `6cebbeb` | feat: add JUnit and SARIF formatters with comprehensive tests for CI/CD integration |
| `4d7d0d1` | feat: add Markdown formatter with table output and guideline documentation links |
| `820d15e` | docs: update progress.md with Markdown formatter milestone |
| `8b7cd4a` | docs: update CLI documentation with all implemented formatters and options |
| `339e318` | docs: add CLI documentation commit to progress.md |
| `94a4eed` | docs: create user-facing CLI reference and refactor AGENTS.md |
| `2ea87d0` | docs: add TOC to CLI reference for easier navigation |
| `b8b1955` | docs: simplify command headings and fix markdown anchors in CLI reference |
| `55d9ed6` | docs: remove CI/CD platform-specific examples from CLI reference |
| `ea6e0a2` | docs: update progress log with CLI docs cleanup completion |
| `857b623` | docs: replace best-practice wording with azure guidelines link |
| `53046f6` | docs: point to user Azure Pipelines guidelines repo |
| `38cfb82` | docs: restructure README to be tool-focused |
| `f4a3b66` | docs: update progress log with README restructure completion |
| `54c4aca` | docs: front-load repository purpose in README intro |
| `d7e2bf3` | docs: update progress log with README intro polish commit |
| `73d8bcf` | docs: add MIT license and PoC disclaimer |
| `276d7a9` | docs: update progress log with license and disclaimer commit |
| `584aba7` | docs: add Mermaid visualizations to reduce cognitive load |
| `42a0009` | docs: update progress log with visual improvements milestone |
| `5ec3b92` | docs: add Mermaid boundary diagrams to Core and Analysis AGENTS files |
| `e13dabb` | feat: add multi-value filter support and clarify severity options |

---

## Implemented rules

All current `ADOG-*` guideline IDs from the companion manifest are already implemented in this
repository.

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

**Guideline automation status recovery and MCP capability planning**

The automation-status documentation and metadata provider were manually reconstructed. The
provider maps guideline IDs to enforceable, heuristic, or not-automatable local capabilities.
The quality gate now starts the MCP host with both the `stdio` and `SSE` launch profiles.

Current focus:
1. Review whether automation status should appear in CLI and MCP guideline lookup output.
2. Prioritize the token-conscious MCP capability work recorded in `docs/TODO.md`.

Validation completed:
- Full solution quality gate passed (`510` passed, `0` failed).
- The MCP `stdio` and `SSE` launch profiles both started successfully.

---

## Next up

1. **Add concise MCP prompts** for reviewing a pipeline, explaining a guideline, and preparing a
   remediation plan.
2. **Consider server-side analysis summaries** grouped by file, category, severity, and rule.
3. **Create the MCP token-usage guide** described in `docs/TODO.md`.
4. **Address remaining CLI documentation debt** in
   `tools/AzurePipelines.Guidelines.Cli` (`AnalyzeCommand`, `RulesCommand`, option resolvers,
   formatters, and exit codes).
5. **Monitor the companion manifest for new `ADOG-*` rules** and add any new ones with the rule
   template workflow when they appear.

---

## Open questions / blockers

- The next MCP capability should be selected from the ordered backlog above at the start of the
  next session.
- NuGet publication is deferred. Package metadata and local packing remain for a future release.
