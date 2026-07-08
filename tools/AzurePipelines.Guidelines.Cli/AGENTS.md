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
- **Output formatters** (in `Formatters/` directory):
  - `IOutputFormatter` interface
  - `OutputFormatterFactory` (resolver)
  - One class per format: `ConsoleOutputFormatter`, `CompactFormatter`, `JsonAnalysisFormatter`, `JunitFormatter`, `SarifFormatter`, `MarkdownFormatter`
- Exit code mapping:
  - `0` = success (no violations at configured severity, or `--soft-fail` mode)
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
- Formatters are **presentation concerns** — they live in the CLI project, not in shared libraries.
- The CLI never modifies input files — it only reads and reports.
- Options follow POSIX conventions: `--format`, `--severity`, `--category`, etc.
- Each formatter implements `IOutputFormatter` and is registered in `OutputFormatterFactory`.

## CLI documentation

**For end-user documentation** (installation, commands, options, examples, CI integration):
→ See [`docs/cli-reference.md`](../../docs/cli-reference.md)

Do **not** duplicate user-facing content here. This file is for agents implementing or modifying the CLI.

## Implementation rules for agents

### 1. Formatter implementation

When adding a new output format:

1. Create a new class implementing `IOutputFormatter` in `Formatters/` directory
2. Register it in `OutputFormatterFactory._formatters` dictionary
3. Add comprehensive unit tests in `tests/.../Formatters/{Name}FormatterTests.cs`
4. Update `docs/cli-reference.md` with format description and examples
5. Ensure strict analyzer compliance (naming conventions, culture-invariant operations)

### 2. Command options

When adding a new CLI option:

1. Define the option in the appropriate command class (`AnalyzeCommand`, `RulesCommand`)
2. Add parameter to command handler
3. Document in `docs/cli-reference.md` with examples
4. Add integration tests covering the new option
5. Update help text to match documentation

### 3. Exit codes

Use the constants from `ExitCodes.cs`:

```csharp
ExitCodes.Success  // 0 - no violations or --soft-fail mode
ExitCodes.Violations  // 1 - violations found
ExitCodes.Error  // 2 - analysis error
```

Never return raw integers. The `--soft-fail` flag forces exit code 0 even when violations exist (audit mode for CI).

### 4. Formatter factory usage

Retrieve formatters via `OutputFormatterFactory.Get(formatName)`:

```csharp
IOutputFormatter formatter = OutputFormatterFactory.Get("json");
string output = formatter.Format(results, useColor: !noColor);
```

The factory throws `ArgumentException` for unknown formats. Supported formats are discoverable via `OutputFormatterFactory.SupportedFormats`.

### 5. Color handling

- Console output respects the `--no-color` flag
- Formatters that don't support colors (JSON, SARIF, etc.) ignore the `useColor` parameter
- Use the `useColor` parameter consistently: `formatter.Format(results, useColor: !noColor)`

## Distribution

Published to NuGet as a .NET global tool. Installed via:

```bash
dotnet tool install -g adog
```

Package ID: `adog` (lowercase, no prefix)
Tool command name: `adog`

## Distribution

Published to NuGet as a .NET global tool. Installed via:

```bash
dotnet tool install -g adog
```

Package ID: `adog` (lowercase, no prefix)
Tool command name: `adog`
