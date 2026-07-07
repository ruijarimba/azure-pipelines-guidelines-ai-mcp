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
| `d72bf64` | feat: add `--category` filter to `adog analyze` and `--severity` filter to `adog rules list` — CLI and MCP |
| `e874e24` | docs: improve MCP tools documentation and fix accuracy issues |
| `428e088` | feat: support multi-file and directory analysis in CLI and MCP |
| `9a037da` | feat: add `adog-mcp` .NET global tool and Docker Hub image distribution |
| `bf07410` | chore: fix CLI packaging and harden CI pack workflow |
| `438da52` | test: raise unit test coverage above 90 % for all assemblies |

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

Nothing — working tree is clean after `d72bf64`.

---

## Next up

1. **Implement remaining `ADOG-*` rules** — cross-reference `guidelines.json` in the companion
   repository against the table above to find unimplemented rule IDs. Use the
   `.github/prompts/implement-rule.prompt.md` workflow for each one.
2. **Publish NuGet packages** for all `src/` libraries — required for Phase 1 completion
   (see `docs/vision.md`).
3. **Verify Phase 1 success criteria** — all rules implemented, `>= 90 %` test coverage,
   documentation complete, global tools and Docker image published.

---

## Open questions / blockers

- How many total rules exist in `guidelines.json`? Fetch the manifest from the companion
  repository at the start of the next session to get an accurate count of remaining work.
- No other blockers known.
