# azure-pipelines-guidelines-ai-mcp

This project provides tools that help you follow Azure Pipelines best practices. It offers both a static analyser you can run in CI/CD and an AI assistant integration.

## What it does

| Tool | Description |
| --- | --- |
| **MCP server** | Exposes guideline lookup and Azure Pipelines YAML analysis as [Model Context Protocol](https://modelcontextprotocol.io) tools and resources |
| **CLI (`adog`)** | Analyses Azure YAML pipeline files against the guidelines; reports violations with fix suggestions |

## Getting started

### Option 1 — CLI static analyser (global tool)

```bash
dotnet tool install -g adog
adog analyze azure-pipelines.yml
adog rules list --category steps
adog rules show ADOG-STEPS-001
```

### Option 2 — MCP server (global tool)

Install and run the MCP server as a .NET global tool. Your MCP client (GitHub Copilot,
Claude Desktop, Cursor, etc.) connects to it over stdio.

```bash
dotnet tool install -g adog-mcp
```

Example MCP client configuration (Claude Desktop `claude_desktop_config.json`):

```json
{
  "mcpServers": {
    "azure-pipelines-guidelines": {
      "command": "adog-mcp"
    }
  }
}
```

### Option 3 — MCP server (Docker, no .NET required)

Run the MCP server as a Docker container. The `-i` flag keeps stdin open, which is
required for the MCP stdio transport.

```bash
docker pull ruijarimba/azure-pipelines-guidelines-mcp:latest
```

Example MCP client configuration using Docker:

```json
{
  "mcpServers": {
    "azure-pipelines-guidelines": {
      "command": "docker",
      "args": ["run", "-i", "--rm", "ruijarimba/azure-pipelines-guidelines-mcp:latest"]
    }
  }
}
```

## Repository layout

```
src/      Core libraries (published as NuGet packages)
tools/    Command-line and server executables
tests/    Unit tests
docs/     Design and architecture documentation
.github/  AI agent instructions and CI workflows
```

See [`docs/architecture.md`](docs/architecture.md) for the full design and dependency graph.

## Companion repository

The guidelines and their machine-readable definitions live in
[ruijarimba/azure-pipelines-guidelines](https://github.com/ruijarimba/azure-pipelines-guidelines).
Each rule has a unique ID following the pattern `ADOG-{CATEGORY}-{NNN}` (for example, `ADOG-STEPS-001`).
