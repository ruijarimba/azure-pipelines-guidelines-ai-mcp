# AGENTS.md — AzurePipelines.Guidelines.Mcp.Host

## Purpose

The **entry point** for the Model Context Protocol (MCP) server. Wires up dependency injection,
registers the MCP host, and starts listening for AI assistant requests.

This is a thin shim — **no business logic lives here**. All analysis, rule, and protocol
logic is in the `src/` class libraries.

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
- `ModelContextProtocol.AspNetCore` (enables the optional HTTP/SSE debug transport)

## Transport modes

The host can run in two modes. Choose the one that matches how the client connects.

### `stdio` — default production mode

- Used by Docker, the .NET global tool (`adog-mcp`), and editor integrations that launch the
  process directly.
- MCP messages travel over `stdout`, so all human-readable logs are forced to `stderr`.
- Start with `--transport stdio` or by selecting the **stdio** launch profile in Visual Studio.

### `SSE` — local debugging mode

- Starts an ASP.NET Core web server so you can debug the live MCP endpoint from an IDE.
- The MCP endpoint is `/mcp` and the configured URL is `http://localhost:5050`.
- Start by selecting the **SSE** launch profile in Visual Studio; this injects the
  `applicationUrl` value from `launchSettings.json` and binds to port 5050.
- Connect VS Code by adding an SSE server pointing to `http://localhost:5050/mcp`.

## Key patterns

- `Program.cs` should stay **5-15 lines**: read transport, dispatch, run.
- All transport-specific startup lives in `McpHostStartup.cs`.
- All configuration is environment-variable or `appsettings.json`-driven — no hard-coded settings.
- Exit codes:
  - `0` = healthy shutdown
  - Non-zero = unhandled exception or startup failure

## Distribution

Configured as the future `adog-mcp` .NET global tool; NuGet publication is deferred. The MCP
server is also packaged as a Docker image for Docker Hub publication.

See `README.md` in this folder for day-to-day build, run, and debug instructions.
