# TODO List

This file tracks upcoming work and enhancements for the next development session.

The backlog below reflects the current state of the repository after the latest validation pass:
- CLI severity terminology and multi-value filter support are implemented.
- MCP documentation and solution registration are complete.
- Folder-based integration tests for the real analysis stack are implemented and validated.
- Local tests, packaging, and documentation validation passed.

## Priority at a glance

| Priority | Item | Why it matters |
| --- | --- | --- |
| Now | Improve executable and runnable-project docs | The MCP host and CLI need clearer guidance for transport, profiles, and options |
| Next | Expand MCP capabilities with token-conscious defaults | This improves client experience without overloading the context window |
| Later | Improve CLI examples and revisit rule coverage | These are useful follow-ups once the main path is stable |

---

## High Priority

### 1. Improve comments and documentation for executable/runnable projects

Many classes, methods, and options lack comments. This is especially problematic for
executable/runnable projects like the MCP host and CLI, where options, profiles, launch
settings, and transport choices control how the program runs.

- [x] Add inline comments explaining non-obvious code in the MCP host (`Program.cs`,
      `McpHostStartup.cs`, launch profiles, transport selection).
- [ ] Add inline comments explaining non-obvious code in the CLI (`AnalyzeCommand`,
      `RulesCommand`, option resolvers, formatters, exit codes).
- [x] Add or update README/AGENTS files that explain:
  - The available launch profiles and when to use each one.
  - The difference between stdio and SSE transports.
  - How to start the MCP host from Visual Studio for live SSE debugging.
  - How to connect VS Code (or another client) to the SSE endpoint.
- [ ] Assume .NET familiarity may be limited; focus on *why* something exists and *how* to use it
      rather than restating syntax.

Related files:
- `tools/AzurePipelines.Guidelines.Mcp.Host/Program.cs`
- `tools/AzurePipelines.Guidelines.Mcp.Host/McpHostStartup.cs`
- `tools/AzurePipelines.Guidelines.Mcp.Host/Properties/launchSettings.json`
- `tools/AzurePipelines.Guidelines.Mcp.Host/README.md`
- `tools/AzurePipelines.Guidelines.Cli/AnalyzeCommand.cs`
- `tools/AzurePipelines.Guidelines.Cli/RulesCommand.cs`
- `docs/mcp-reference.md`
- `docs/cli-reference.md`

---

## Medium Priority

### 2. Add the remaining future enhancements

These are optional follow-ups that are not blocked by the current implementation.

- [x] Add `--output` support for multiple formats when `--format` contains more than one value
- [x] Add a dedicated "Environment Variables" section to `docs/cli-reference.md`
- [x] Consider config-file support for CLI defaults
- [x] Review and commit the current handoff notes in `docs/TODO.md` and `docs/progress.md` before the next session

Related files:
- `tools/AzurePipelines.Guidelines.Cli/AnalyzeCommand.cs`
- `docs/cli-reference.md`
- `docs/mcp-reference.md`

---

### 3. Revisit rule coverage if the companion manifest changes

The current manifest scan found no missing `ADOG-*` IDs to implement, so this remains a watch item rather than an active task.

- [ ] Re-check `guidelines.json` when the companion repository changes
- [ ] Implement any newly introduced rules with the existing rule-implementation workflow

Related files:
- `docs/progress.md`
- `.github/prompts/implement-rule.prompt.md`

---

### 4. Expand MCP capabilities with token-conscious defaults

Improve MCP support without making clients load the full guideline catalogue or detailed rule
content by default.

- [ ] Surface `automationStatus` and its reason in single-guideline tools and resources by using
      `IGuidelineAutomationMetadataProvider`
- [ ] Add focused resources for guideline categories and guideline automation metadata
- [ ] Add concise MCP prompts for reviewing a pipeline, explaining a guideline, and preparing a
      remediation plan
- [ ] Add server-side analysis summaries that group diagnostics by file, category, severity, and
      rule
- [ ] Consider an `explain_diagnostic` tool when the raw diagnostic and fix guidance are not
      sufficient for AI clients
- [ ] Consider `analyze_changed_pipelines` for pull-request review workflows, with strict
      workspace-path boundaries
- [ ] Do not add Sampling or LLM-assisted heuristic analysis as part of this work

Related files:
- `src/AzurePipelines.Guidelines.Core/IGuidelineAutomationMetadataProvider.cs`
- `src/AzurePipelines.Guidelines.Rules/GuidelineAutomationMetadataProvider.cs`
- `src/AzurePipelines.Guidelines.Mcp/Tools/GuidelineTools.cs`
- `src/AzurePipelines.Guidelines.Mcp/Tools/PipelineAnalysisTools.cs`
- `src/AzurePipelines.Guidelines.Mcp/Resources/GuidelineResources.cs`
- `docs/mcp-reference.md`

---

### 5. Document MCP token usage

Create a user-facing document that explains how each MCP capability affects client token usage.
Include practical guidance for keeping usage low: use focused lookups instead of full catalogues,
return summaries by default, make detailed results opt-in, limit and page list results, omit empty
fields, and keep prompts procedural. Explain that full-catalogue resources and large analysis
results are the main token risks.

Related files:
- `docs/mcp-reference.md`
- `docs/` token-usage document to be created and registered in `AzurePipelinesGuidelines.slnx`

---

## Low Priority / Future Enhancements

### 6. Improve CLI documentation examples

Add more end-to-end examples for CI/CD, scripting, and mixed-format output scenarios.

Related files:
- `docs/cli-reference.md`
- `docs/mcp-reference.md`

---

## Recent Session Summary

**Date:** 2026-07-13

**Completed:**
- ✅ Added inline comments to the MCP host (`Program.cs`, `McpHostStartup.cs`, `launchSettings.json`) explaining transport selection, stderr-only logging, and launch profile URL injection.
- ✅ Created `tools/AzurePipelines.Guidelines.Mcp.Host/README.md` with build/run/debug instructions for stdio and SSE modes, registered in the host `.csproj`.
- ✅ Updated host and MCP library `AGENTS.md` files with transport and handler guidance.
- ✅ Added inline comments to MCP library source (`GuidelinesMcpServiceCollectionExtensions.cs`, `GuidelineResources.cs`, `GuidelineTools.cs`, `PipelineAnalysisTools.cs`) for non-obvious choices.
- ✅ Verified the solution with the full quality gate (`491` tests passed).

**Status:**
- NuGet publication is deferred; package configuration remains for a future release
- Docker Hub publication for the MCP server remains in scope
- Docker-based container validation remains environment-dependent and is not required for the current backlog

**Files changed:**
- `docs/mcp-reference.md`
- `docs/cli-reference.md`
- `tools/AzurePipelines.Guidelines.Cli/AnalyzeCommand.cs`
- `tools/AzurePipelines.Guidelines.Cli/AnalyzeCommandOptionResolver.cs`
- `tools/AzurePipelines.Guidelines.Cli/RulesCommand.cs`
- `tools/AzurePipelines.Guidelines.Mcp/Tools/PipelineAnalysisTools.cs`
- `tests/AzurePipelines.Guidelines.Cli.Tests/AnalyzeCommandTests.cs`

---

## Notes

- Review and prioritize this list at the start of each session.
- Move completed work from this file to `docs/progress.md` when it becomes part of the release record.
