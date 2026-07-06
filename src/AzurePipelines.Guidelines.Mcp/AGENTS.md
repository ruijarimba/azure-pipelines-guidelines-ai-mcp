# AGENTS.md — AzurePipelines.Guidelines.Mcp

## Purpose

Implements the Model Context Protocol (MCP) server logic. Exposes guideline lookup and
Azure Pipelines YAML analysis as MCP **tools** and **resources** that AI assistants can call.

## What belongs here

- MCP tool handler implementations (guideline lookup, YAML analysis, fix suggestions).
- MCP resource definitions (guideline catalogue, per-rule metadata).
- DI extension method: `AddGuidelinesMcp(IServiceCollection)`.
- Request/response DTOs specific to the MCP surface — these are `internal` and must not
  bleed into the domain model in `Core`.

## What does NOT belong here

- Business logic → `Core` / `Analysis`
- Host / process lifecycle → `Mcp.Host`
- Rule implementations → `Rules`
- Direct YAML parsing — use `IAnalysisEngine` from `Analysis`

## Dependencies (internal)

- `AzurePipelines.Guidelines.Core`
- `AzurePipelines.Guidelines.Analysis`

## Dependencies (NuGet)

- `ModelContextProtocol`
- `Microsoft.Extensions.Hosting.Abstractions`
- `Microsoft.Extensions.Logging.Abstractions`

## Key patterns

- Each MCP tool is a separate `internal sealed` class annotated with the MCP SDK tool attribute.
- Tool handlers depend on `IAnalysisEngine` and `IGuidelineRepository` via constructor injection.
- Only the DI extension method (`AddGuidelinesMcp`) is `public`; all handler classes are `internal`.
- Tool descriptions shown to AI clients must be concise, accurate, and derived from the
  guideline manifest vocabulary.

## Adding a new tool

Follow the guided workflow in `.github/prompts/add-mcp-tool.prompt.md`.
