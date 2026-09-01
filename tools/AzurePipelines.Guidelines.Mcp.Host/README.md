# AzurePipelines.Guidelines.Mcp.Host

This project is the executable entry point for the Azure Pipelines Guidelines MCP server. It exposes the guideline catalog and YAML analysis logic as [Model Context Protocol](https://modelcontextprotocol.io/introduction) (MCP) tools and resources.

## What this project is

The host is a thin wrapper built on the [official C# MCP SDK](https://github.com/modelcontextprotocol/csharp-sdk). It starts the MCP server, chooses a transport, and then gets out of the way. The actual MCP tools, resources, rules, and parsing live in the `src/` class libraries.

## What you need to know before running

The host supports two transport modes. You must pick the one that matches how your MCP client connects.

| Transport | When to use it | How messages travel |
| --- | --- | --- |
| **stdio** | A local MCP client launches the process. This includes Docker and editor integrations that start a command. | Over `stdin` and `stdout`. All logs go to `stderr` so they do not corrupt the protocol. |
| **HTTP transport** | A client connects to an already-running server. This includes local debugging and remote hosting. | HTTP at `/mcp`. MCP 2.0 serves the modern Streamable HTTP transport here by default, while the legacy HTTP+SSE path remains available for compatibility when the host is used in a trusted local-debugging workflow. |

The executable defaults to **stdio**. This default supports process-launching clients; it is not a general preference over HTTP. The **Debug** launch profile is the Visual Studio-friendly entry point for the HTTP transport and starts the host on the same `/mcp` endpoint. The older **SSE** profile remains available as a compatibility alias for existing workflows.

## Container runtime

The Docker image uses `mcr.microsoft.com/dotnet/aspnet:10.0`, not `mcr.microsoft.com/dotnet/runtime:10.0`. This is required because the host references `ModelContextProtocol.AspNetCore` for the HTTP transport. That package requires the `Microsoft.AspNetCore.App` shared framework.

.NET checks required shared frameworks when the process starts. It does this even when you run the host in `stdio` mode. The smaller `runtime` image does not include `Microsoft.AspNetCore.App`, so the process would exit before it could start the stdio server.

The project intentionally publishes **one ASP.NET runtime image** for both `stdio` and HTTP.

| Image approach | Result | Maintenance cost |
| --- | --- | --- |
| One `aspnet` image | Supports `stdio`, HTTP, Streamable HTTP, legacy SSE compatibility, and hosted deployments. | One build, test path, image tag, publish step, and set of user instructions. |
| Two images based on `runtime` and `aspnet` | A smaller stdio-only image plus a separate HTTP image. | Two builds, test paths, image tags, publish steps, version checks, and sets of user instructions. |

Using the base `runtime` image would require removing or splitting the HTTP features. The project keeps one image so every supported transport runs from the same tested executable.

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

### From Visual Studio with stdio

1. Set **AzurePipelines.Guidelines.Mcp.Host** as the startup project.
2. Select the **stdio** launch profile in the toolbar.
3. Press `F5`.

## Run the HTTP transport

The HTTP transport lets you debug live MCP requests from Visual Studio while a supported client connects to the server over HTTP.

### From Visual Studio with HTTP

1. Set **AzurePipelines.Guidelines.Mcp.Host** as the startup project.
2. Select the **Debug** launch profile in the toolbar.
3. Press `F5`.

The **Debug** profile is the recommended path for local debugging from Visual Studio. The older **SSE** profile remains available as a compatibility alias.

The launch profile binds the server to `http://localhost:5050`. The MCP endpoint is at:

```text
http://localhost:5050/mcp
```

### Why the launch profile matters

The `applicationUrl` value in `launchSettings.json` is injected when you choose an HTTP launch profile, such as **Debug** or **SSE**. If you start the project without one of those profiles, ASP.NET Core falls back to its default URL (typically port `5000`). Always select an HTTP profile when debugging over the HTTP transport.

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

Then start the host with the **Debug** launch profile. A client version that supports the HTTP transport will list the available tools and resources once the server is running.

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
