# TODO List

This file tracks upcoming work and enhancements for the next development session.

---

## High Priority

### 1. Fix severity terminology inconsistency

Two different "severity" concepts cause confusion in the CLI:

**Problem:**
- `adog analyze --severity`: Uses **diagnostic severity** (`error`, `warning`, `info`) — filters violation output by how serious they are when reported
- `adog rules list --severity`: Uses **guideline severity** (`do`, `do-not`, `avoid`, `consider`) — filters rules by their imperative strength from the manifest

**Why both exist:**
These are legitimately different concepts:
- **Guideline severity** = the imperative strength of the rule itself (from `guidelines.json`)
- **Diagnostic severity** = how violations are reported in analysis output (mapped via `GuidelineSeverityExtensions.ToDiagnosticSeverity()`)

**Solutions to consider:**
- [ ] Update `docs/cli-reference.md` to add a section explaining the distinction between diagnostic and guideline severity
- [ ] Consider renaming `rules list --severity` to `--guideline-severity` or `--imperative` for clarity
- [ ] Add a "Severity Concepts" section to CLI docs with examples
- [ ] Cross-reference `docs/glossary.md` severity mapping table

**Related files:**
- `tools/AzurePipelines.Guidelines.Cli/AnalyzeCommand.cs` (lines 243-249: ParseSeverity method)
- `tools/AzurePipelines.Guidelines.Cli/RulesCommand.cs` (lines 16-18, 87-98: severity parsing and filtering)
- `src/AzurePipelines.Guidelines.Core/GuidelineSeverityExtensions.cs` (lines 15-24: mapping)
- `docs/cli-reference.md` (lines 63, 197: user-facing documentation)
- `docs/glossary.md` (severity mapping reference)

---

### 2. Support multiple values for filter options

Allow users to specify multiple categories, severities, or other filter values in a single command.

**Desired behavior:**

```bash
# Multiple categories (comma-separated)
adog analyze pipeline.yml --category general,jobs,steps

# OR multiple flags (either syntax should work)
adog analyze pipeline.yml --category general --category jobs --category steps

# Multiple diagnostic severities for analyze
adog analyze pipeline.yml --severity error,warning

# Multiple guideline severities for rules list
adog rules list --severity do,do-not
```

**Implementation checklist:**
- [ ] Update `AnalyzeCommand` option parsing to accept `string[]` instead of `string?` for `--category`
- [ ] Update `RulesCommand` to accept arrays for `--category` and `--severity`
- [ ] Modify `AnalyzeCommandOptionResolver` to handle comma-separated values and multiple flags
- [ ] Update `AnalyzeCommandEnvironment` to parse comma-separated env vars: `ADOG_CATEGORY="general,jobs,steps"`
- [ ] Apply same pattern to MCP tools in `PipelineAnalysisTools.cs`
- [ ] Add unit tests for multi-value scenarios in `AnalyzeCommandTests.cs`
- [ ] Update `docs/cli-reference.md` with examples of multiple values

**Benefits:**
- More flexible filtering without running multiple commands
- Better CI/CD integration (analyze specific rule subsets)
- Improved user experience

**Related files:**
- `tools/AzurePipelines.Guidelines.Cli/AnalyzeCommand.cs` (lines 32-35: category option definition)
- `tools/AzurePipelines.Guidelines.Cli/RulesCommand.cs` (lines 32-40: filter options)
- `tools/AzurePipelines.Guidelines.Cli/AnalyzeCommandOptionResolver.cs` (all resolver methods)
- `tools/AzurePipelines.Guidelines.Cli/AnalyzeCommandEnvironment.cs` (env var parsing)
- `tools/AzurePipelines.Guidelines.Mcp/Tools/PipelineAnalysisTools.cs` (MCP tool parameters)
- `tests/AzurePipelines.Guidelines.Cli.Tests/AnalyzeCommandTests.cs`

---

## Medium Priority

### 3. Implement remaining ADOG rules

Cross-reference `guidelines.json` in the companion repository (https://github.com/ruijarimba/azure-pipelines-guidelines) against the implemented rules list in `docs/progress.md`.

**Currently implemented:** 9 rules
- `ADOG-GENERAL-001` → `RelativeTemplatePathRule`
- `ADOG-JOBS-001` → `JobMissingCheckoutRule`
- `ADOG-JOBS-006` → `JobMissingTimeoutRule`
- `ADOG-PARAMETERS-002` → `ParameterMissingValuesRule`
- `ADOG-STEPS-001` → `MacroSyntaxInStepsRule`
- `ADOG-STEPS-006` → `StepMissingTimeoutRule`
- `ADOG-STEPS-010` → `LargeExpressionInStepsRule`
- `ADOG-STEPS-011` → `AzureKeyVaultTaskRule`
- `ADOG-VARIABLES-003` → `SecretLikeVariableRule`

**Workflow:**
Follow `.github/prompts/implement-rule.prompt.md` for each new rule.

**Prerequisites:**
Fetch the latest `guidelines.json` manifest from the companion repository at the start of the session to identify remaining rules.

---

### 4. Add multi-flag CLI integration tests

Create integration tests that verify multiple CLI options work correctly together.

**Test scenarios:**
- [ ] `--format json,markdown --output report` (multiple formats with file output)
- [ ] `--soft-fail --severity error --category steps` (audit mode with filters)
- [ ] `--no-color --quiet --format compact` (output modifiers together)
- [ ] `--verbose --format sarif --output results.sarif` (verbose with SARIF)
- [ ] Environment variable + CLI flag precedence with multiple options
- [ ] Invalid combinations that should produce clear error messages

**Why important:**
Catch interaction bugs and edge cases that unit tests might miss.

**Related files:**
- `tests/AzurePipelines.Guidelines.Cli.Tests/AnalyzeCommandTests.cs`

---

### 5. Publish NuGet packages

Required for Phase 1 completion (see `docs/vision.md`).

**Packages to publish:**
- `AzurePipelines.Guidelines.Core`
- `AzurePipelines.Guidelines.Parsing`
- `AzurePipelines.Guidelines.Rules`
- `AzurePipelines.Guidelines.Analysis`
- `AzurePipelines.Guidelines.Mcp` (optional, for SDK consumers)

**Prerequisites:**
- [ ] All unit tests passing (✅ currently passing)
- [ ] Test coverage >= 90% for all assemblies (✅ currently above 90%)
- [ ] Documentation complete and accurate
- [ ] Follow NuGet packaging rules (see `.github/instructions/nuget-packaging.instructions.md`)
- [ ] Version numbers synchronized via `Directory.Build.props`
- [ ] README.md and LICENSE files included in packages

**Verification:**
Run Phase 1 success criteria checklist from `docs/vision.md`.

---

## Low Priority / Future Enhancements

### 6. Add `--output` support for multiple formats

When `--format` has multiple values, write each to a separate file with pattern-based naming.

**Examples:**

```bash
# Pattern-based naming
adog analyze pipeline.yml --format json,sarif,junit --output report.{format}
# Creates: report.json, report.sarif, report.junit

# Explicit file list (same order as formats)
adog analyze pipeline.yml --format json,sarif --output report.json --output results.sarif
```

**Current behavior:**
Multiple formats write to stdout sequentially (all mixed together).

**Benefits:**
- Clean separation of different format outputs
- Easier CI/CD artifact collection
- Better tooling integration

---

### 7. Environment variable documentation improvements

Add dedicated `## Environment Variables` section in `docs/cli-reference.md`.

**Current state:**
Environment variables are documented inline in the options table (column 4).

**Proposed improvement:**
Create a dedicated section showing:
- All supported `ADOG_*` variables in one place
- Precedence rules (CLI > env > default)
- Boolean value formats (`true/false`, `1/0`, `yes/no`)
- Examples for each variable
- Cross-reference to options table

**Benefits:**
- Easier to find all env vars at a glance
- Better CI/CD integration documentation
- Clearer for users configuring environments

---

### 8. Consider config file support

Allow users to specify CLI options in a configuration file.

**File formats to consider:**
- JSON: `adog.json` or `.adogrc.json`
- YAML: `adog.yml` or `.adogrc.yml`
- TOML: `adog.toml`
- INI-style: `.adogrc`

**Precedence:**
CLI flag > Environment variable > Config file > Built-in default

**Benefits:**
- Team-wide consistent analysis configuration
- Easier CI/CD setup (commit config to repo)
- Less command-line verbosity
- Project-specific rule configuration

**Example config (JSON):**

```json
{
  "analyze": {
    "severity": "warning",
    "category": ["jobs", "steps"],
    "format": ["console", "sarif"],
    "softFail": false,
    "noColor": false
  }
}
```

**Discovery:**
Search for config file in:
1. Current directory
2. Parent directories (walk up to repo root)
3. User home directory (~/.adogrc)

---

## Recent Session Summary

**Date:** 2025-01-XX

**Completed:**
- ✅ Implemented CLI environment variable support with `ADOG_*` prefix
- ✅ Added precedence resolution: CLI flag > env var > default
- ✅ Created `AnalyzeCommandEnvironment` for env parsing and boolean validation
- ✅ Created `AnalyzeCommandOptionResolver` for centralized option resolution
- ✅ Added unit tests for env fallback, precedence, invalid values, and soft-fail (4/4 passing)
- ✅ Updated `docs/cli-reference.md` with environment variable column in options table
- ✅ Replaced Mermaid command overview diagram with compact summary table in CLI reference

**Identified issues:**
- 🔍 Severity terminology inconsistency between `analyze` and `rules list` commands
- 💡 Need for multiple value support in filter options (`--category`, `--severity`)

**Status:**
Ready to commit. All tests passing, documentation updated.

**Files changed:**
- `tools/AzurePipelines.Guidelines.Cli/AnalyzeCommand.cs`
- `tools/AzurePipelines.Guidelines.Cli/AnalyzeCommandEnvironment.cs` (new)
- `tools/AzurePipelines.Guidelines.Cli/AnalyzeCommandOptionResolver.cs` (new)
- `tests/AzurePipelines.Guidelines.Cli.Tests/AnalyzeCommandTests.cs`
- `docs/cli-reference.md`

---

## Notes

- This TODO list should be reviewed and prioritized at the start of each session.
- Items can be promoted/demoted between priority levels as project needs evolve.
- When an item is completed, move it to `docs/progress.md` under "Recently completed" with the commit hash.
