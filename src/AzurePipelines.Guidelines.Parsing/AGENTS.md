# AGENTS.md — AzurePipelines.Guidelines.Parsing

## Purpose

Transforms raw Azure Pipelines YAML text into the `PipelineDocument` AST defined in `Core`.
**This is the only project in the solution that knows about YamlDotNet.**

## What belongs here

- Implementation of `IPipelineParser` from `Core`.
- YamlDotNet-specific mapping from YAML nodes to `Core` AST types.
- Structural validation of the parsed document (malformed YAML, unrecognised top-level keys).
- `PipelineParsingException` — thrown when YAML cannot be mapped to a valid pipeline document.

## What does NOT belong here

- Rule logic or diagnostic generation → `Rules` / `Analysis`
- Knowledge of guideline IDs → `Core` / `Rules`
- MCP protocol code → `Mcp`

## Dependencies (internal)

- `AzurePipelines.Guidelines.Core`

## Dependencies (NuGet)

- `YamlDotNet`

## Key patterns

- The only public API surface is the `IPipelineParser` implementation.
  All YamlDotNet types are `internal` implementation details and must **not** appear in
  any public signature.
- Parsing is **pure and deterministic**: given the same YAML text, always produce the same AST.
- On unrecognised YAML that cannot be mapped, throw `PipelineParsingException` (defined here)
  rather than silently returning a partial document.
- Use `internal sealed` for all mapping/helper classes.
