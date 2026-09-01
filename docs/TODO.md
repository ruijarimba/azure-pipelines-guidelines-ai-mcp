# TODO List

This file tracks upcoming work and optional enhancements for future development sessions.

The backlog below reflects the current state of the repository after the latest validation pass:

- The MCP server and shared analysis stack are implemented.
- MCP documentation and solution registration are complete.
- Folder-based integration tests for the real analysis stack are implemented and validated.
- Local tests, packaging, and documentation validation passed.

## Priority at a glance

| Priority | Item | Why it matters |
| --- | --- | --- |
| Watch | Revisit rule coverage | Check for new rules when the companion manifest changes |

---

## High Priority

### 1. Improve comments and documentation for the MCP host

Many classes, methods, and options lack comments. This is especially problematic for the MCP host, where options, profiles, launch settings, and transport choices control how the program runs.

- [x] Add inline comments explaining non-obvious code in the MCP host (`Program.cs`,
      `McpHostStartup.cs`, launch profiles, transport selection).
- [x] Add or update README/AGENTS files that explain:
  - The available launch profiles and when to use each one.
  - The difference between stdio and SSE transports.
  - How to start the MCP host from Visual Studio for live SSE debugging.
  - How to connect VS Code (or another client) to the SSE endpoint.
- [x] Assume .NET familiarity may be limited; focus on *why* something exists and *how* to use it
      rather than restating syntax.

Related files:

- `tools/AzurePipelines.Guidelines.Mcp.Host/Program.cs`
- `tools/AzurePipelines.Guidelines.Mcp.Host/McpHostStartup.cs`
- `tools/AzurePipelines.Guidelines.Mcp.Host/Properties/launchSettings.json`
- `tools/AzurePipelines.Guidelines.Mcp.Host/README.md`
- `docs/mcp-reference.md`

---

## Medium Priority

### 2. Record optional future enhancements

These are optional follow-ups that are not blocked by the current implementation.

- [x] Review and commit the current handoff notes in `docs/TODO.md` and `docs/progress.md` before the next session

Related files:

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

Improve MCP support without making clients load the full guideline catalogue or detailed rule content by default.

- [x] Surface `automationStatus` and its reason in single-guideline tools and resources by using
      `IGuidelineAutomationMetadataProvider`
- [x] Add focused resources for guideline automation metadata
- [x] Add cache-friendly guideline catalogue version and category resources
- [x] Make `get_guideline` return summaries by default and require explicit full detail for the
      detailed payload
- [x] Add concise MCP prompts for reviewing a pipeline, explaining a guideline, and preparing a
      remediation plan
- [x] Add a cacheable `adog://capabilities` resource for server, catalogue, transport, and MCP
      surface discovery
- [x] Add server-side analysis summaries that group diagnostics by file, category, severity, and
      rule
- [x] Update predefined prompts to present guideline recommendations as DO, DO-NOT, AVOID, and
      CONSIDER instead of diagnostic severity labels
- [x] Add an `explain_diagnostic` tool that returns one guideline's full detail by ID, optionally
      echoing back the diagnostic message, file path, line, and column that raised it

Related files:

- `src/AzurePipelines.Guidelines.Core/IGuidelineAutomationMetadataProvider.cs`
- `src/AzurePipelines.Guidelines.Rules/GuidelineAutomationMetadataProvider.cs`
- `src/AzurePipelines.Guidelines.Mcp/Tools/GuidelineTools.cs`
- `src/AzurePipelines.Guidelines.Mcp/Tools/PipelineAnalysisTools.cs`
- `src/AzurePipelines.Guidelines.Mcp/Resources/GuidelineResources.cs`
- `src/AzurePipelines.Guidelines.Mcp/Resources/GuidelineAutomationMetadataDto.cs`
- `src/AzurePipelines.Guidelines.Mcp/Tools/GuidelineAutomationMetadataDto.cs`
- `docs/mcp-reference.md`

---

### 5. Document MCP token usage

Create a user-facing document that explains how each MCP capability affects client token usage. Include practical guidance for keeping usage low: use focused lookups instead of full catalogues, return summaries by default, make detailed results opt-in, scope list results with available filters, omit empty fields, and keep prompts procedural. Explain that full-catalogue resources and large analysis results are the main token risks.

- [x] Create `docs/mcp-token-usage.md` and register it in `AzurePipelinesGuidelines.slnx`

Related files:

- `docs/mcp-reference.md`
- `docs/mcp-token-usage.md`

---

## Recent validation summary

The current branch contains the completed MCP server, shared analysis stack, documentation, and Solution Explorer registration. The latest feature commit is `36a34a3`; the branch handoff and agent guidance were updated in `eab7f24`, `7201554`, and `55f428f`.

**Completed:**

- ✅ Updated predefined MCP prompts to render guideline recommendations as `DO`, `DO-NOT`, `AVOID`,
  and `CONSIDER` instead of diagnostic severity labels.
- ✅ Validated the change with targeted MCP prompt tests (`10` passed, `0` failed) and the full
  repository quality gate.
- ✅ Fixed `global.json` to roll forward to newer installed .NET 10 SDK bands so Docker builds
  (`dotnet/sdk:10.0`) and local builds both resolve successfully.
- ✅ Audited `docs/` for staleness and consistency; `docs/mcp-token-usage.md` is registered in
  `AzurePipelinesGuidelines.slnx`.

**Status:**

- NuGet publication is out of scope; package configuration remains for local builds only
- Docker Hub publication for the MCP server is complete

---

## Notes

- Review and prioritize this list at the start of each session.
- Move completed work from this file to `docs/progress.md` when it becomes part of the release record.
