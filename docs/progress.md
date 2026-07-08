# Work in Progress

This file is the **session handoff note** for AI agents and human contributors.
Update it before every commit so the next session starts with accurate context.

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

---

## Implemented rules

9 of the rules from `guidelines.json` are implemented.
Check the companion repository for the full manifest to identify gaps.

| Rule ID | Class |
| --- | --- |
| `ADOG-GENERAL-001` | `RelativeTemplatePathRule` |
| `ADOG-JOBS-001` | `JobMissingCheckoutRule` |
| `ADOG-JOBS-006` | `JobMissingTimeoutRule` |
| `ADOG-PARAMETERS-002` | `ParameterMissingValuesRule` |
| `ADOG-STEPS-001` | `MacroSyntaxInStepsRule` |
| `ADOG-STEPS-006` | `StepMissingTimeoutRule` |
| `ADOG-STEPS-010` | `LargeExpressionInStepsRule` |
| `ADOG-STEPS-011` | `AzureKeyVaultTaskRule` |
| `ADOG-VARIABLES-003` | `SecretLikeVariableRule` |

New rule template: follow `.github/prompts/implement-rule.prompt.md`.

---

## In progress

**Visual documentation improvements** ✅ **COMPLETED**

Completed a systematic visual documentation rollout to reduce cognitive load:
- ✅ Replaced ASCII dependency graph in root `AGENTS.md` with Mermaid architecture flowchart
- ✅ Added agent-behavior decision flowchart to root `AGENTS.md`
- ✅ Added Mermaid Gantt roadmap to `docs/vision.md`
- ✅ Added Mermaid mindmaps to Core/AGENTS.md (what belongs / doesn't belong)
- ✅ Added Mermaid mindmaps to Analysis/AGENTS.md (orchestration boundaries)
- ✅ Added command-tree diagram to `docs/cli-reference.md`
- ✅ Added tool-capability matrix diagram to `docs/mcp-reference.md`

All agent and user-facing documentation now has visual diagrams to clarify architecture,
boundaries, and tool capabilities.

Next: implement remaining rules or other enhancements.

---

## Next up

1. **Implement remaining `ADOG-*` rules** — cross-reference `guidelines.json` in the companion
   repository against the implemented rules table above. Use the
   `.github/prompts/implement-rule.prompt.md` workflow for each one.
2. **CLI: support comma-separated `--format` values** — e.g. `--format json,markdown`.
   Each format is written to stdout in sequence (or to separate files once `--output` is added).
3. **Add multi-flag CLI integration tests** — test combinations of `--format`, `--output`, `--soft-fail`, `--no-color`
4. **Publish NuGet packages** for all `src/` libraries — required for Phase 1 completion
   (see `docs/vision.md`).
5. **Verify Phase 1 success criteria** — all rules implemented, `>= 90 %` test coverage,
   documentation complete, global tools and Docker image published.

---

## Open questions / blockers

- How many total rules exist in `guidelines.json`? Fetch the manifest from the companion
  repository at the start of the next session to get an accurate count of remaining work.
- No other blockers known.
