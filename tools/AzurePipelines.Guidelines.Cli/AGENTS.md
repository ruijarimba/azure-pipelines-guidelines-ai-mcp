# AGENTS.md — AzurePipelines.Guidelines.Cli

## Purpose

The **CLI static analyzer** (`adog`) for Azure Pipelines YAML files. Analyzes pipelines
against the guideline ruleset and outputs violations with fix suggestions. Designed to run
in CI pipelines or developer workstations.

Published as a **.NET global tool** via `dotnet tool install -g adog`.

## What belongs here

- `Program.cs` — `System.CommandLine` root command + subcommands.
- Commands:
  - `analyze <path> [<path> ...]` — run analysis on one or more YAML files or directories, output diagnostics
  - `rules list` — list all available rules
  - `rules show <rule-id>` — show details for a specific rule
- Output formatters: console (default), JSON, SARIF.
- Exit code mapping:
  - `0` = no violations at the configured severity threshold
  - `1` = violations found
  - `2` = analysis error (invalid YAML, file not found, etc.)

## What does NOT belong here

- Rule implementations → `Rules`
- Analysis orchestration → `Analysis`
- YAML parsing → `Parsing`

## Dependencies (internal)

- `AzurePipelines.Guidelines.Analysis` (which transitively brings `Rules`, `Parsing`, `Core`)

## Dependencies (NuGet)

- `System.CommandLine`
- `Microsoft.Extensions.Hosting`
- `Microsoft.Extensions.Logging`
- `Microsoft.Extensions.Logging.Console`

## Key patterns

- All commands inject `IAnalysisEngine` — no direct coupling to parsing or rules.
- Output is **machine-parseable by default** (JSON, SARIF) with optional human-friendly console mode.
- The CLI never modifies input files — it only reads and reports.
- Options follow POSIX conventions: `--format`, `--severity`, `--category`, etc.

## CLI surface

### `adog analyze <path> [<path> ...]`

Analyse one or more pipeline files or directories against the guidelines.
Directories are scanned recursively for `*.yml` / `*.yaml` files.

| Option | Values | Default | Description |
| --- | --- | --- | --- |
| `--format` | `console`, `compact`, `json`, `junit`, `sarif`, `markdown` | `console` | Output format. Accepts a comma-separated list to produce multiple formats in one run. |
| `--severity` | `error`, `warning`, `info` | `info` | Minimum severity to report. `error` reports only errors; `info` reports everything. |
| `--category` | `general`, `jobs`, `parameters`, `pipelines`, `stages`, `steps`, `variables` | _(all)_ | Limit analysis to a single guideline category. |
| `--output`, `-o` | file path | _(stdout)_ | Write output to a file instead of stdout. |
| `--soft-fail` | (flag) | `false` | Always exit with code 0, even if violations are found (audit mode for CI). |
| `--no-color` | (flag) | `false` | Disable ANSI color codes in console output. |
| `--quiet`, `-q` | (flag) | `false` | Suppress detailed output, show summary only. |
| `--verbose`, `-v` | (flag) | `false` | Enable detailed logging. |

**Examples**

```bash
# Analyse a single file
adog analyze pipeline.yml

# Analyse multiple files
adog analyze pipeline.yml templates/build-job.yml

# Analyse an entire directory (recursive)
adog analyze ./pipelines/

# Mix files and directories
adog analyze pipeline.yml ./templates/ ./shared/

# Report only errors
adog analyze pipeline.yml --severity error

# Report errors and warnings, category steps only
adog analyze ./pipelines/ --severity warning --category steps

# JSON output (machine-readable; useful in CI scripts)
adog analyze pipeline.yml --format json

# Compact output (grep-parseable, one line per violation)
adog analyze pipeline.yml --format compact

# JUnit XML output (for CI test results publication)
adog analyze pipeline.yml --format junit

# Markdown report with links to guideline documentation
adog analyze pipeline.yml --format markdown

# SARIF output (integrates with GitHub Code Scanning and Azure DevOps)
adog analyze pipeline.yml --format sarif

# Produce both JSON and Markdown in a single run
adog analyze ./pipelines/ --format json,markdown

# Produce all formats at once
adog analyze ./pipelines/ --format console,compact,json,junit,sarif,markdown

# Write output to a file instead of stdout
adog analyze pipeline.yml --format json --output report.json

# Audit mode: analyse and report but always exit 0 (useful in CI)
adog analyze ./pipelines/ --soft-fail

# Disable colors for plain text output (useful in CI logs)
adog analyze pipeline.yml --no-color

# Quiet mode: show summary only, suppress detailed diagnostics
adog analyze ./pipelines/ --quiet

# Verbose mode: enable detailed logging
adog analyze ./pipelines/ --verbose

# Full example: steps violations, errors only, JSON to file, audit mode
adog analyze ./pipelines/ --category steps --severity error --format json --output report.json --soft-fail
```

---

### `adog rules list`

List all guidelines loaded from the manifest.

| Option | Values | Default | Description |
| --- | --- | --- | --- |
| `--category` | `general`, `jobs`, `parameters`, `pipelines`, `stages`, `steps`, `variables` | _(all)_ | Filter by category. |
| `--severity` | `do`, `do-not`, `avoid`, `consider` | _(all)_ | Filter by guideline severity. |
| `--format` | `console`, `json`, `markdown` | `console` | Output format. Accepts a comma-separated list. |

**Examples**

```bash
# List all rules
adog rules list

# List rules for a single category
adog rules list --category steps

# List only mandatory rules (do / do-not)
adog rules list --severity do
adog rules list --severity do-not

# List advisory rules for jobs
adog rules list --category jobs --severity consider

# JSON output
adog rules list --format json

# Markdown table with links to guideline documentation
adog rules list --format markdown

# Produce both console and JSON
adog rules list --category steps --format console,json
```

---

### `adog rules show <rule-id>`

Show full details for a single guideline rule.

| Option | Values | Default | Description |
| --- | --- | --- | --- |
| `--format` | `console`, `json`, `markdown` | `console` | Output format. Accepts a comma-separated list. |

**Examples**

```bash
# Show a rule in human-readable format
adog rules show ADOG-STEPS-001

# JSON output (includes all fields from the manifest)
adog rules show ADOG-STEPS-001 --format json

# Markdown output with a link to the guideline documentation
adog rules show ADOG-STEPS-001 --format markdown

# Produce both JSON and Markdown
adog rules show ADOG-STEPS-001 --format json,markdown
```

---

## Output formats

| Format | Description | Primary use |
| --- | --- | --- |
| `console` | Human-readable text with file grouping and summary statistics; colored when the terminal supports it. | Developer workstation |
| `compact` | One line per violation in grep-parseable format: `file:line:col: severity: [ruleId] message` | CI logs, grep/awk pipelines |
| `json` | Machine-readable JSON with summary and per-file diagnostics (camelCase properties). | CI scripts, downstream tooling |
| `junit` | JUnit XML format with test cases per file; violations appear as failures/errors. | CI test results publication (Azure Pipelines, GitHub Actions) |
| `sarif` | [SARIF 2.1.0](https://sarifweb.azurewebsites.net/) — integrates with GitHub Code Scanning, Azure DevOps, and the VS Code SARIF Viewer extension. | GitHub / Azure DevOps PR annotations |
| `markdown` | Markdown tables with summary metrics and per-file violations; rule IDs link to guideline documentation. | Pull request comments, wiki pages, reports |

Multiple formats can be requested in a single run by passing a comma-separated list:
`--format json,markdown,sarif`.

---

## Exit codes

| Code | Meaning |
| --- | --- |
| `0` | Success: no violations at or above the configured `--severity` threshold, or `--soft-fail` mode enabled. |
| `1` | Violations found (unless `--soft-fail` is used). |
| `2` | Analysis error: invalid YAML, file not found, unknown option value, etc. |

---

## Planned options (not yet implemented)

These options are planned for future releases. Do not implement them until they appear in
`docs/progress.md` under "In progress".

| Option | Description |
| --- | --- |
| `--include <glob>` | Include only files matching a glob pattern when scanning directories. |
| `--exclude <glob>` | Exclude files matching a glob pattern when scanning directories. |
| `--config <path>` | Load option defaults from a `.adog.yml` configuration file (Phase 2). |
| `--rule <rule-id>` | Limit analysis to a single rule ID; may be repeated for multiple rules. |

## Distribution

Published to NuGet as a .NET global tool. Installed via:

```bash
dotnet tool install -g adog
```
