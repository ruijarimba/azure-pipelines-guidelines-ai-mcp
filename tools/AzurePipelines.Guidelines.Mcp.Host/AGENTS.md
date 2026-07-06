# AGENTS.md — AzurePipelines.Guidelines.Mcp.Host

## Purpose

The **entry point** for the Model Context Protocol (MCP) server. Wires up dependency injection,
registers the MCP host, and starts listening for AI assistant requests.

This is a thin shim — **no business logic lives here**. All analysis, rule, and protocol
logic is in the `src/` class libraries.

## What belongs here

- `Program.cs` — host builder, DI registration (`AddGuidelinesAnalysis`, `AddGuidelinesMcp`), startup.
- Optional: `appsettings.json` for configuration (logging levels, MCP server options).

## What does NOT belong here

- Rule implementations
- Parsing logic
- MCP tool handlers
- Any domain or protocol logic — everything flows through injected services from `Mcp`

## Dependencies (internal)

- `AzurePipelines.Guidelines.Mcp` (which transitively brings `Analysis` → `Rules`, `Parsing`, `Core`)

## Dependencies (NuGet)

- `Microsoft.Extensions.Hosting` (full runtime)
- `Microsoft.Extensions.Logging.Console`

## Key patterns

- `Program.cs` should be **5-15 lines**: build host, wire DI, run.
- All configuration is environment-variable or `appsettings.json`-driven — no hard-coded settings.
- Exit codes:
  - `0` = healthy shutdown
  - Non-zero = unhandled exception or startup failure

## Distribution

Published as a **standalone executable** (self-contained or framework-dependent).
Also packaged as a container image for deployment to cloud workloads.
Not published to NuGet — this is a runtime artifact, not a library.
