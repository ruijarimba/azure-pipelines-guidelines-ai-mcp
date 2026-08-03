# AzurePipelines.Guidelines.Mcp.Host

This project is the executable entry point for the Azure Pipelines Guidelines MCP server.
It exposes the guideline catalog and YAML analysis logic as [Model Context Protocol](https://modelcontextprotocol.io/introduction) (MCP) tools and resources.

## What this project is

The host is a thin wrapper. It starts the MCP server, chooses a transport, and then gets out
of the way. The actual MCP tools, resources, rules, and parsing live in the `src/` class
libraries.

## What you need to know before running

The host supports two transport modes. You must pick the one that matches how your MCP client
connects.

| Transport | When to use it | How messages travel |
| --- | --- | --- |
| **stdio** | A local MCP client launches the process. This includes Docker and editor integrations that start a command. | Over `stdin` and `stdout`. All logs go to `stderr` so they do not corrupt the protocol. |
| **HTTP transport** | A client connects to an already-running server. This includes local debugging and remote hosting. | HTTP at `/mcp`. MCP 2.0 serves the modern Streamable HTTP transport here by default, while the legacy HTTP+SSE path remains available for compatibility when the host is used in a trusted local-debugging workflow. |

The executable defaults to **stdio**. This default supports process-launching clients; it is not
a general preference over HTTP. The **Debug** launch profile is the Visual Studio-friendly
entry point for the HTTP transport and starts the host on the same `/mcp` endpoint. The older
**SSE** profile remains available as a compatibility alias for existing workflows.

## Build the host

From the repository root:

```powershell
dotnet build tools/AzurePipelines.Guidelines.Mcp.Host/AzurePipelines.Guidelines.Mcp.Host.csproj -c Release
```

## Run in stdio mode

### From the command line

This is how Docker and editor integrations start the server:

```powershell
dotnet run --project tools/AzurePipelines.Guidelines.Mcp.Host -- --transport stdio
```

Or via the packed tool:

```powershell
dotnet tool install -g adog-mcp
adog-mcp
```

### From Visual Studio

1. Set **AzurePipelines.Guidelines.Mcp.Host** as the startup project.
2. Select the **stdio** launch profile in the toolbar.
3. Press `F5`.

## Run the HTTP transport

The HTTP transport lets you debug live MCP requests from Visual Studio while a supported client
connects to the server over HTTP.

### From Visual Studio

1. Set **AzurePipelines.Guidelines.Mcp.Host** as the startup project.
2. Select the **Debug** launch profile in the toolbar.
3. Press `F5`.

The **Debug** profile is the recommended path for local debugging from Visual Studio. The older
**SSE** profile remains available as a compatibility alias.

The launch profile binds the server to `http://localhost:5050`. The MCP endpoint is at:

```text
http://localhost:5050/mcp
```

### Why the launch profile matters

The `applicationUrl` value in `launchSettings.json` is only injected when you choose the
**SSE** launch profile. If you start the project without that profile, ASP.NET Core falls back
to its default URL (typically port `5000`). Always select the profile when debugging over the
HTTP transport.

### From the command line for quick testing

```powershell
dotnet run --project tools/AzurePipelines.Guidelines.Mcp.Host --launch-profile Debug -- --transport sse
```

You can also pass `--urls` explicitly:

```powershell
dotnet run --project tools/AzurePipelines.Guidelines.Mcp.Host -- --transport sse --urls "http://localhost:5050"
```

## Connect a client to the HTTP endpoint

Add a server entry to your user `mcp.json`:

```json
{
  "servers": {
    "adog-sse-debug": {
      "type": "http",
      "url": "http://localhost:5050/mcp"
    }
  }
}
```

Then start the host with the **Debug** launch profile. A client version that supports the HTTP
transport will list the available tools and resources once the server is running.

## Switch transport with an environment variable

If you cannot use command line arguments, set `MCP_TRANSPORT`:

```powershell
$env:MCP_TRANSPORT = "SSE"
dotnet run --project tools/AzurePipelines.Guidelines.Mcp.Host
```

The command line flag `--transport` takes priority over the environment variable.

## Logging behavior

- In **stdio** mode, all logs are written to `stderr`. `stdout` is reserved for MCP traffic.
- In **HTTP** mode, logs are written to `stderr` by default and are visible in the Visual Studio
  debug output.

If you see no startup logs, check that the log level is set to `Information` or lower.

## Run with Docker Compose

The Docker image uses Streamable HTTP by default and listens on port `8080` inside the container.
From the repository root, start the published service with the Compose wrapper:

```powershell
pwsh ./scripts/run-mcp-compose.ps1
```

The MCP endpoint is `http://localhost:8080/mcp`. The Compose wrapper does not read `.env` when it
runs the MCP server. Docker Hub credentials are used only by `scripts/publish-mcp-image.ps1`; they
are not passed to the running container.
