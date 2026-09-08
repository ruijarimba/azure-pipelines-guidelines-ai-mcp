# AGENTS.md — AzurePipelines.Guidelines.Mcp.Host

## Purpose

The **entry point** for the Model Context Protocol (MCP) server. Wires up dependency injection, registers the MCP host, and starts listening for AI assistant requests.

This is a thin shim — **no business logic lives here**. All analysis, rule, and protocol logic is in the `src/` class libraries.

## What belongs here

- `Program.cs` — resolves the transport (`stdio` or `SSE`) from `--transport` or
  `MCP_TRANSPORT`, then calls the matching startup path.
- `McpHostStartup.cs` — hosts the MCP server; uses `Microsoft.Extensions.Hosting` for stdio
  transport and ASP.NET Core for SSE transport.
- `Properties/launchSettings.json` — Visual Studio launch profiles so you can start the host
  in the correct transport with `F5`.
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
- `ModelContextProtocol.AspNetCore` (enables the optional HTTP transport for local debugging and hosted deployments)

## Transport modes

The host supports two transport modes. Choose the one that matches how the client connects.

### `stdio` — local process transport

- Used when Docker, the .NET global tool (`adog-mcp`), or an editor integration starts the
  process directly on the local machine.
- MCP messages travel over `stdin` and `stdout`, so all human-readable logs are forced to
  `stderr`.
- Start with `--transport stdio` or by selecting the **stdio** launch profile in Visual Studio.

### HTTP — already-running server transport

- Starts an ASP.NET Core web server for Visual Studio debugging or hosted deployments.
- The MCP endpoint is `/mcp` and the local development URL is `http://localhost:5050`.
- Start by selecting the **Debug** launch profile in Visual Studio; the legacy **SSE** name is
  retained as a compatibility alias, but the **Debug** profile is the recommended entry point for
  local debugging and injects `applicationUrl` from `launchSettings.json`.
- Connect an MCP client that supports the HTTP transport to `http://localhost:5050/mcp`.
- Configure HTTPS, authentication, authorization, and network access controls before exposing
  the endpoint beyond the local machine.

## Key patterns

- `Program.cs` should stay **5-15 lines**: read transport, dispatch, run.
- All transport-specific startup lives in `McpHostStartup.cs`.
- All configuration is environment-variable or `appsettings.json`-driven — no hard-coded settings.
- Exit codes:
  - `0` = healthy shutdown
  - Non-zero = unhandled exception or startup failure

## Distribution

Retains local packaging configuration for the `adog-mcp` .NET global tool; this repository does not publish NuGet packages. The MCP server is also packaged as a Docker image for Docker Hub publication.

See `README.md` in this folder for day-to-day build, run, and debug instructions.
