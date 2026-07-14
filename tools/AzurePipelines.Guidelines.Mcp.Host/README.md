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
| **stdio** | Production, Docker, the `adog-mcp` CLI tool, and any editor that launches the process directly. | Over `stdout`. All logs go to `stderr` so they do not corrupt the protocol. |
| **SSE** | Local debugging only. Starts a small ASP.NET Core web server so you can inspect live requests in an IDE. | Over HTTP/SSE at `/mcp`. |

The default transport is **stdio**. SSE is only used when you explicitly select it.

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

## Run in SSE debug mode

SSE lets you debug live MCP requests from Visual Studio while clients such as VS Code talk to
the server over HTTP.

### From Visual Studio

1. Set **AzurePipelines.Guidelines.Mcp.Host** as the startup project.
2. Select the **SSE** launch profile in the toolbar.
3. Press `F5`.

The launch profile binds the server to `http://localhost:5050`. The MCP endpoint is at:

```text
http://localhost:5050/mcp
```

### Default URL behavior

The `urls` setting in `appsettings.json` configures SSE as `http://localhost:5050` whether it
is started through the **SSE** launch profile or directly with `--transport sse`. Pass `--urls`
or set `ASPNETCORE_URLS` to use a different URL.

### From the command line for quick testing

```powershell
dotnet run --project tools/AzurePipelines.Guidelines.Mcp.Host --launch-profile SSE -- --transport sse
```

You can also pass `--urls` explicitly:

```powershell
dotnet run --project tools/AzurePipelines.Guidelines.Mcp.Host -- --transport sse --urls "http://localhost:5050"
```

## Connect VS Code to the SSE endpoint

Add a server entry to your user `mcp.json`:

```json
{
  "servers": {
    "adog-sse-debug": {
      "type": "sse",
      "url": "http://localhost:5050/mcp"
    }
  }
}
```

Then start the host with the **SSE** launch profile. VS Code will list the available tools and
resources once the server is running.

## Switch transport with an environment variable

If you cannot use command line arguments, set `MCP_TRANSPORT`:

```powershell
$env:MCP_TRANSPORT = "SSE"
dotnet run --project tools/AzurePipelines.Guidelines.Mcp.Host
```

The command line flag `--transport` takes priority over the environment variable.

## Logging behavior

- In **stdio** mode, all logs are written to `stderr`. `stdout` is reserved for MCP traffic.
- In **SSE** mode, logs are written to `stderr` by default and are visible in the Visual Studio
  debug output.

If you see no startup logs, check that the log level is set to `Information` or lower.
