# Architecture

## Overview

This project is a layered .NET 10 solution that builds two tools on top of
the [azure-pipelines-guidelines](https://github.com/ruijarimba/azure-pipelines-guidelines)
machine-readable definitions:

1. **MCP server** — AI assistants call it to look up guidelines and analyze pipeline YAML files.
2. **CLI static analyzer** (`adog`) — runs in CI or locally to flag violations.

## Dependency graph

```
AzurePipelines.Guidelines.Core
│
├── AzurePipelines.Guidelines.Parsing
│    └── [NuGet] YamlDotNet
│
├── AzurePipelines.Guidelines.Rules
│
├── AzurePipelines.Guidelines.Analysis
│    └── [NuGet] Microsoft.Extensions.DependencyInjection.Abstractions
│
└── AzurePipelines.Guidelines.Mcp
     └── [NuGet] ModelContextProtocol
     └── [NuGet] Microsoft.Extensions.Hosting.Abstractions

tools/AzurePipelines.Guidelines.Mcp.Host  [exe]
     └── AzurePipelines.Guidelines.Mcp
     └── [NuGet] Microsoft.Extensions.Hosting

tools/AzurePipelines.Guidelines.Cli  [exe]
     └── AzurePipelines.Guidelines.Analysis
     └── [NuGet] System.CommandLine
     └── [NuGet] Microsoft.Extensions.Hosting
```

**Rule:** arrows point from dependent → dependency. Cycles are forbidden.
`Core` has no internal project dependencies.

## Layer responsibilities

| Layer | Owns | Must not contain |
| --- | --- | --- |
| `Core` | Domain models, interfaces, enums, value objects | Parsing, rule logic, I/O, NuGet beyond BCL |
| `Parsing` | YAML → AST transformation via YamlDotNet | Rule logic, diagnostic generation |
| `Rules` | `IRule` implementations keyed by `ADOG-…` ID | YAML parsing, cross-rule state, I/O |
| `Analysis` | Orchestration: parse → filter → run → aggregate | YAML details, protocol code, console I/O |
| `Mcp` | MCP tool/resource handlers, DI extension methods | Rule logic, direct YAML parsing, host lifecycle |
| `Mcp.Host` | Host wiring, DI registration, config | All business logic |
| `Cli` | Commands, output formatters, exit-code mapping | All business logic |

## Key interfaces (defined in Core)

| Interface | Purpose |
| --- | --- |
| `IPipelineParser` | Parses YAML text into a `PipelineDocument` |
| `IRule` | Analyzes a `PipelineDocument` and returns `Diagnostic` instances |
| `IGuidelineRepository` | Loads `GuidelineDefinition` records from the manifest |
| `IAnalysisEngine` | Orchestrates parsing and rules to produce `AnalysisResult` |

## Extension points

| Goal | Where to add |
| --- | --- |
| New lint rule | Implement `IRule` in `Rules` and register via DI |
| New MCP tool | Add handler class in `Mcp` |
| New CLI command | Add `Command` subclass in `Cli` |
| New output format | Add formatter in `Cli` (for example, SARIF) |
| Alternative YAML parser | Replace `IPipelineParser` implementation in `Parsing` |

## Guideline manifest

Rule ID pattern:

```
ADOG-(GENERAL|JOBS|PARAMETERS|PIPELINES|STAGES|STEPS|VARIABLES)-[0-9]{3}
```

For severity mapping to diagnostic level, detection kinds, and all domain terms,
see [`glossary.md`](glossary.md).

## Build infrastructure

| File | Purpose |
| --- | --- |
| `global.json` | Pins .NET SDK version |
| `Directory.Build.props` (root) | TFM, nullable, TreatWarningsAsErrors, AnalysisLevel |
| `src/Directory.Build.props` | NuGet metadata, GenerateDocumentationFile |
| `tools/Directory.Build.props` | Disables IsPackable, GenerateDocumentationFile |
| `tests/Directory.Build.props` | Disables IsPackable, suppresses CA1707 |
| `Directory.Packages.props` | Central NuGet version management |
| `.editorconfig` | C# code style rules |

## CLI surface (planned)

```
adog analyze <path> [--format console|json|sarif] [--severity error|warning|info]
adog rules list [--category <category>]
adog rules show <rule-id>
```

Exit codes: `0` = no violations at threshold, `1` = violations found, `2` = analysis error.

## NuGet packages

All `src/` projects are published as independent packages (`AzurePipelines.Guidelines.*`).
`Cli` is published as a .NET global tool via `dotnet tool install`.
`Mcp.Host` is distributed as a standalone executable or container image.
