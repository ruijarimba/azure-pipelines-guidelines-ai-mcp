# CLI Reference — `adog`

The `adog` command-line tool analyzes Azure Pipelines YAML files against the [coding guidelines](https://github.com/ruijarimba/azure-pipelines-guidelines) and reports violations with fix suggestions.

## Command Overview

| Command | Purpose |
| --- | --- |
| `adog analyze <path>...` | Analyze pipeline files against guidelines |
| `adog rules list` | List all available rules |
| `adog rules show <id>` | Show details for a specific rule |

## Table of Contents

- [Local use](#local-use)
- [Commands](#commands)
  - [`adog analyze`](#adog-analyze)
  - [`adog rules list`](#adog-rules-list)
  - [`adog rules show`](#adog-rules-show)
- [Configuration Files](#configuration-files)
- [Environment Variables](#environment-variables)
- [Output Formats](#output-formats)
- [Exit Codes](#exit-codes)
- [Guideline Categories](#guideline-categories)
- [Troubleshooting](#troubleshooting)
- [See Also](#see-also)

## Local use

NuGet publication is deferred. Clone the repository and run the CLI from the repository root:

```bash
dotnet run --project tools/AzurePipelines.Guidelines.Cli -- analyze azure-pipelines.yml
```

The `adog` global-tool package configuration remains in the project for a future NuGet release.

## Commands

### `adog analyze`

**Syntax:**
```
adog analyze <path> [<path> ...]
```

Analyze one or more pipeline files or directories against the guidelines.
Directories are scanned recursively for `*.yml` and `*.yaml` files.

#### Options

| Option | Type | Default | Environment variable | Description |
| --- | --- | --- | --- | --- |
| `--format` | string | `console` | `ADOG_FORMAT` | Output format: `console`, `compact`, `json`, `junit`, `sarif`, `markdown`. Accepts comma-separated list for multiple formats. |
| `--severity` | string | `info` | `ADOG_SEVERITY` | Minimum severity to report: `error`, `warning`, or `info`. Accepts a comma-separated list or repeated flags. |
| `--category` | string | _(all)_ | `ADOG_CATEGORY` | Limit analysis to one or more categories: `general`, `jobs`, `parameters`, `pipelines`, `stages`, `steps`, or `variables`. Accepts a comma-separated list or repeated flags. |
| `--output`, `-o` | path | _(stdout)_ | `ADOG_OUTPUT` | Write output to file instead of stdout. |
| `--soft-fail` | flag | `false` | `ADOG_SOFT_FAIL` | Always exit with code 0, even if violations are found (audit mode for CI). Boolean env values: `true`/`false`, `1`/`0`, `yes`/`no`. |
| `--no-color` | flag | `false` | `ADOG_NO_COLOR` | Disable ANSI color codes in console output. Boolean env values: `true`/`false`, `1`/`0`, `yes`/`no`. |
| `--quiet`, `-q` | flag | `false` | `ADOG_QUIET` | Suppress detailed output, show summary only. Boolean env values: `true`/`false`, `1`/`0`, `yes`/`no`. |
| `--verbose`, `-v` | flag | `false` | `ADOG_VERBOSE` | Enable detailed logging. Boolean env values: `true`/`false`, `1`/`0`, `yes`/`no`. |
| `--include-heuristics` | flag | `false` | _(none)_ | Include advisory heuristic rules. These rules can be noisy because YAML alone cannot prove every violation. |

The analyzer always runs `enforceable` rules. It does not run `notAutomatable` rules. Use
`--include-heuristics` only when you want optional advisory findings:

```bash
adog analyze pipeline.yml --include-heuristics
```

See [guideline automation status](guideline-automation.md) for the status and reason for every rule.

#### Configuration files

You can keep reusable defaults in a JSON config file. The CLI looks for these files in this order:
1. `./adog.json`
2. `./.adogrc.json`
3. `~/adog.json`
4. `~/.adogrc.json`

Example config file:

```json
{
  "format": "json,markdown",
  "severity": "warning",
  "category": "steps,jobs",
  "output": "report.json",
  "soft-fail": true,
  "no-color": true,
  "quiet": false,
  "verbose": false
}
```

The CLI resolves settings in this order:
1. Explicit CLI option
2. Environment variable
3. Configuration file
4. Built-in default

#### Environment variables

Supported environment variables:

| Variable | Applies to | Example |
| --- | --- | --- |
| `ADOG_FORMAT` | `adog analyze --format` | `ADOG_FORMAT=json,markdown` |
| `ADOG_SEVERITY` | `adog analyze --severity` | `ADOG_SEVERITY=warning` |
| `ADOG_CATEGORY` | `adog analyze --category` | `ADOG_CATEGORY=steps,jobs` |
| `ADOG_OUTPUT` | `adog analyze --output` | `ADOG_OUTPUT=report.json` |
| `ADOG_SOFT_FAIL` | `adog analyze --soft-fail` | `ADOG_SOFT_FAIL=true` |
| `ADOG_NO_COLOR` | `adog analyze --no-color` | `ADOG_NO_COLOR=1` |
| `ADOG_QUIET` | `adog analyze --quiet` | `ADOG_QUIET=yes` |
| `ADOG_VERBOSE` | `adog analyze --verbose` | `ADOG_VERBOSE=false` |

Boolean environment variables accept `true/false`, `1/0`, or `yes/no`. If a boolean value is invalid, `adog` exits with code `2` and prints an error.

Configuration values use the same option names as the CLI flags and support the same accepted values. If a config value is invalid, the CLI prints an error and exits with code `2`.

Example:

```bash
ADOG_CATEGORY=steps,jobs ADOG_SEVERITY=warning adog analyze ./pipelines/
```

#### Examples

**Basic usage:**

```bash
# Analyze a single file
adog analyze pipeline.yml

# Analyze multiple files
adog analyze pipeline.yml templates/build-job.yml

# Analyze entire directory (recursive)
adog analyze ./pipelines/

# Mix files and directories
adog analyze pipeline.yml ./templates/ ./shared/
```

**Filtering:**

```bash
# Report only errors (suppress warnings and info)
adog analyze pipeline.yml --severity error

# Report errors and warnings for steps category only
adog analyze ./pipelines/ --severity warning --category steps

# Analyze only jobs-related rules
adog analyze pipeline.yml --category jobs

# List rules by guideline severity (this is different from analyze --severity)
adog rules list --guideline-severity do
```

**Output formats:**

```bash
# Console output (default, human-readable with colors)
adog analyze pipeline.yml

# Compact format (one line per violation, grep-parseable)
adog analyze pipeline.yml --format compact

# JSON output (machine-readable for CI scripts)
adog analyze pipeline.yml --format json

# JUnit XML (for CI test results publication)
adog analyze pipeline.yml --format junit

# SARIF (for GitHub Code Scanning / Azure DevOps)
adog analyze pipeline.yml --format sarif

# Markdown report (for PRs or documentation)
adog analyze pipeline.yml --format markdown

# Multiple formats at once (comma-separated)
adog analyze pipeline.yml --format json,markdown
adog analyze ./pipelines/ --format console,compact,json,junit,sarif,markdown
```

**File output:**

```bash
# Write JSON output to file
adog analyze pipeline.yml --format json --output report.json

# Write SARIF to file
adog analyze pipeline.yml --format sarif --output results.sarif
```

**CI/CD integration:**

```bash
# Audit mode: analyze but always exit 0 (don't fail the build)
adog analyze ./pipelines/ --soft-fail

# Disable colors for plain text CI logs
adog analyze pipeline.yml --no-color

# Quiet mode: summary only, suppress detailed diagnostics
adog analyze ./pipelines/ --quiet

# Verbose mode: detailed logging
adog analyze ./pipelines/ --verbose
```

**Complex examples:**

```bash
# Steps violations only, errors only, JSON to file, audit mode
adog analyze ./pipelines/ \
  --category steps \
  --severity error \
  --format json \
  --output report.json \
  --soft-fail

# Generate all formats for CI pipeline
adog analyze ./pipelines/ \
  --format json,junit,sarif \
  --output results

# Compact format with no colors for grep
adog analyze ./pipelines/ --format compact --no-color | grep "ADOG-STEPS"
```

---

### `adog rules list`

**Syntax:**
```
adog rules list [options]
```

List all available guideline rules.

#### Options

| Option | Type | Default | Description |
| --- | --- | --- | --- |
| `--category` | string | _(all)_ | Filter by category: `general`, `jobs`, `parameters`, `pipelines`, `stages`, `steps`, or `variables`. |
| `--guideline-severity`, `--severity` | string | _(all)_ | Filter by guideline severity: `do`, `do-not`, `avoid`, or `consider`. `--severity` is kept as a compatibility alias. |
| `--format` | string | `console` | Output format: `console` or `json`. |

#### Examples

```bash
# List all rules
adog rules list

# List rules for a specific category
adog rules list --category steps

# List only mandatory rules
adog rules list --severity do
adog rules list --severity do-not

# List advisory rules for jobs
adog rules list --category jobs --severity consider

# JSON output
adog rules list --format json

# JSON output with structured data
adog rules list --category steps --format json
```

---

### `adog rules show`

**Syntax:**
```
adog rules show <rule-id> [options]
```

Show detailed information for a specific rule.

#### Options

| Option | Type | Default | Description |
| --- | --- | --- | --- |
| `--format` | string | `console` | Output format: `console` or `json`. |

#### Examples

```bash
# Show a rule in human-readable format
adog rules show ADOG-STEPS-001

# JSON output (includes all manifest fields)
adog rules show ADOG-STEPS-001 --format json

# JSON output with structured data
adog rules show ADOG-STEPS-001 --format json
```

---

## Output Formats

The `--format` option controls how analysis results are presented. All formats contain the same diagnostic information but optimize for different use cases. The `markdown` format is available for `adog analyze`; the `adog rules list` and `adog rules show` subcommands currently support `console` and `json` only.

| Format | Description | Best for |
| --- | --- | --- |
| `console` | Human-readable output with file grouping, severity icons, and summary statistics. Includes ANSI colors when terminal supports them. | Developer workstation, interactive terminal sessions |
| `compact` | One line per violation in format: `file:line:col: severity: [ruleId] message`. Parseable by `grep`, `awk`, and other text tools. | CI logs, Unix pipelines, scripts |
| `json` | Structured JSON with summary metrics and per-file diagnostics (camelCase properties, indented). | CI scripts, downstream tooling, programmatic consumption |
| `junit` | JUnit XML format with test cases per file. Violations appear as `<failure>` (warnings/info) or `<error>` (errors). | CI test results publication (Azure Pipelines, GitHub Actions, Jenkins) |
| `sarif` | [SARIF 2.1.0](https://sarifweb.azurewebsites.net/) with tool metadata, rules, locations, and severity levels. | GitHub Code Scanning, Azure DevOps PR annotations, VS Code SARIF Viewer |
| `markdown` | Markdown tables with summary metrics and per-file violations. Rule IDs link to guideline documentation. | Pull request comments, wiki pages, HTML reports |

### Multiple Formats

You can request multiple formats in a single run:

```bash
adog analyze pipeline.yml --format json,markdown,sarif
```

Each selected format is rendered in order and concatenated with blank lines into a single output stream, or into a single file when `--output` is used.

---

## Exit Codes

The CLI uses standard Unix exit codes to signal analysis results:

| Code | Meaning |
| --- | --- |
| `0` | Success: no violations at or above the configured `--severity` threshold, or `--soft-fail` mode enabled. |
| `1` | Violations found (unless `--soft-fail` is used). |
| `2` | Analysis error: invalid YAML, file not found, unknown option value, etc. |

### Using Exit Codes in CI

**Fail the build on violations:**

```bash
# Exit 1 if any violations found
adog analyze ./pipelines/ --severity error
```

**Audit mode (never fail the build):**

```bash
# Always exit 0, even if violations exist
adog analyze ./pipelines/ --soft-fail
```

**Conditional logic:**

```bash
# Bash example
if adog analyze pipeline.yml --severity error; then
  echo "Pipeline is clean"
else
  echo "Violations found"
  exit 1
fi
```

---

## Guideline Categories

The `--category` filter limits analysis to rules in a single category:

| Category | Covers |
| --- | --- |
| `general` | Pipeline-wide structural rules (file organization, template paths) |
| `jobs` | Job definition guidance (timeouts, checkout steps) |
| `parameters` | Parameter declarations and validation |
| `pipelines` | Pipeline-level settings and triggers |
| `stages` | Stage structure and dependencies |
| `steps` | Step and task guidance (variable usage, expressions, task configuration) |
| `variables` | Variable declarations, naming, and security |

For the full rule list and definitions, see the [Azure Pipelines Guidelines repository](https://github.com/ruijarimba/azure-pipelines-guidelines).

---

## Troubleshooting

### "Command not found" after installation

Add the .NET tools directory to your PATH:

**Windows (PowerShell):**
```powershell
$env:PATH += ";$env:USERPROFILE\.dotnet\tools"
```

**Linux/macOS:**
```bash
export PATH="$PATH:$HOME/.dotnet/tools"
```

### YAML parsing errors

If analysis fails with a YAML parsing error, check:
- File is valid YAML (try `yamllint` or an online validator)
- File encoding is UTF-8
- No tabs (Azure Pipelines YAML must use spaces)

### No violations found but expecting some

Check:
- `--severity` threshold (e.g., `--severity error` hides warnings)
- `--category` filter (may be excluding the rule you expect)
- Rule is implemented (see `adog rules list` for available rules)

---

## See Also

- [Azure Pipelines Guidelines](https://github.com/ruijarimba/azure-pipelines-guidelines) — rule definitions and manifest
- [MCP Server Documentation](mcp-reference.md) — AI assistant integration
- [Architecture Guide](architecture.md) — how the tool works internally
- [Contributing Guide](../CONTRIBUTING.md) — how to add rules or features
