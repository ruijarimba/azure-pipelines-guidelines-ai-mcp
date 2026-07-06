# AGENTS.md — AzurePipelines.Guidelines.Analysis

## Purpose

Orchestrates the full analysis pipeline: parse YAML → filter applicable rules → run rules →
aggregate diagnostics into an `AnalysisResult`. This is the primary entry point for both
the MCP server and the CLI tool.

## What belongs here

- Implementation of `IAnalysisEngine` from `Core`.
- `AnalysisResult` record (list of `Diagnostic`, summary statistics, elapsed time).
- `AnalysisOptions` — filtering parameters (category, minimum severity, scopes).
- DI extension method: `AddGuidelinesAnalysis(IServiceCollection)`.
- Rule-filtering logic (by category, severity, `appliesTo` scope).

## What does NOT belong here

- YAML parsing details — use `IPipelineParser` injected from `Core`.
- Rule implementations — they are injected as `IEnumerable<IRule>`.
- MCP protocol concerns → `Mcp`.
- Console or file I/O.

## Dependencies (internal)

- `AzurePipelines.Guidelines.Core`
- `AzurePipelines.Guidelines.Parsing`
- `AzurePipelines.Guidelines.Rules`

## Dependencies (NuGet)

- `Microsoft.Extensions.DependencyInjection.Abstractions` (for `IServiceCollection`)
- `Microsoft.Extensions.Logging.Abstractions` (for `ILogger<T>`)

## Key patterns

- `IAnalysisEngine` is the **single public seam** for all callers (`Mcp`, `Cli`).
- Rules are resolved at runtime via `IEnumerable<IRule>` — adding a new rule to the DI
  container automatically makes it available to the engine.
- The engine never modifies pipeline files; it produces read-only results only.
- All public methods returning collections return `IReadOnlyList<T>`.
- `AddGuidelinesAnalysis` registers the parser, all rules, and the engine — callers only
  need one call to wire up the full analysis stack.
