# TODO List

This file tracks upcoming work and enhancements for the next development session.

The backlog below reflects the current state of the repository after the latest validation pass:
- CLI severity terminology and multi-value filter support are implemented.
- MCP documentation and solution registration are complete.
- Folder-based integration tests for the real analysis stack are implemented and validated.
- Local tests, packaging, and documentation validation passed.

---

## High Priority

No active high-priority work is planned. NuGet package metadata and local packing remain in
place for a future release. NuGet publication is out of scope for the near term.

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

## Low Priority / Future Enhancements

### 4. Improve CLI documentation examples

Add more end-to-end examples for CI/CD, scripting, and mixed-format output scenarios.

Related files:
- `docs/cli-reference.md`
- `docs/mcp-reference.md`

---

## Recent Session Summary

**Date:** 2026-07-11

**Completed:**
- ✅ Implemented multi-value filtering for `adog analyze` and `adog rules list`
- ✅ Added the `--guideline-severity` alias and clarified the severity distinction in docs
- ✅ Added MCP reference documentation and registered it in the solution
- ✅ Implemented multi-format output support for `adog analyze --format ...` with combined output to `--output`
- ✅ Added config-file support for CLI defaults from `adog.json` / `.adogrc.json`
- ✅ Verified the solution with tests and local packaging

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
