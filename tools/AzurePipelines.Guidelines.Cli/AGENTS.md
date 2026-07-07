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

## CLI surface (planned)

```bash
adog analyze <path> [<path> ...] [--format console|json] [--severity error|warning|info]
adog rules list [--category <category>] [--format console|json]
adog rules show <rule-id> [--format console|json]
```

## Distribution

Published to NuGet as a .NET global tool. Installed via:

```bash
dotnet tool install -g adog
```
