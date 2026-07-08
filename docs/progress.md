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
| (pending) | feat: add Markdown formatter with table output and guideline documentation links |

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

**CLI output formatters enhancement** ✅ **COMPLETED**

All six formatters (console, compact, json, junit, sarif, markdown) are now implemented with comprehensive tests:
- ✅ `IOutputFormatter` interface and `OutputFormatterFactory`
- ✅ `ExitCodes.Success` rename for audit-mode semantics
- ✅ `AnalyzeCommand` CLI flags: `--output`, `--soft-fail`, `--no-color`, `--quiet`, `--verbose`
- ✅ `ConsoleOutputFormatter` (ANSI colors, grouping, summary)
- ✅ `CompactFormatter` (grep-parseable one-line format)
- ✅ `JsonAnalysisFormatter` (camelCase structured output)
- ✅ `JunitFormatter` (XML for CI test results)
- ✅ `SarifFormatter` (SARIF 2.1.0 for code scanning)
- ✅ `MarkdownFormatter` (tables with guideline links)
- ✅ All 87 formatter tests passing
- ✅ Build green with strict analyzers
- ✅ Fixed naming conventions (private const fields now use `_` prefix)

Next: commit and push this milestone, then continue with remaining CLI work.

See active plan in memory for detailed step breakdown.

---

## Next up

1. **Wire formatters into `AnalyzeCommand` execution pipeline** — connect factory/file-output/soft-fail logic end-to-end
2. **Add multi-flag CLI integration tests** — test combinations of `--format`, `--output`, `--soft-fail`, `--no-color`
3. **Update CLI documentation** — refresh examples with new formatters and flags
4. **Implement remaining `ADOG-*` rules** — cross-reference `guidelines.json` in the companion
   repository against the implemented rules table above. Use the
   `.github/prompts/implement-rule.prompt.md` workflow for each one.
5. **CLI: support comma-separated `--format` values** — e.g. `--format json,markdown`.
   Each format is written to stdout in sequence (or to separate files once `--output` is added).
6. **Publish NuGet packages** for all `src/` libraries — required for Phase 1 completion
   (see `docs/vision.md`).
7. **Verify Phase 1 success criteria** — all rules implemented, `>= 90 %` test coverage,
   documentation complete, global tools and Docker image published.

---

## Open questions / blockers

- How many total rules exist in `guidelines.json`? Fetch the manifest from the companion
  repository at the start of the next session to get an accurate count of remaining work.
- No other blockers known.
