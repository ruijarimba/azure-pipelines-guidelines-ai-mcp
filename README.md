# Azure Pipelines Guidelines MCP server

![Status: proof of concept](https://img.shields.io/badge/status-proof--of--concept-orange)

This is a PoC MCP server that can be used by AI assistants to analyze Azure Pipelines YAML against the [Azure Pipelines coding guidelines](https://github.com/ruijarimba/azure-pipelines-guidelines).

It returns rule-backed diagnostics for pipelines and steps, jobs, stages, and variables templates with stable rule IDs and fix suggestions.

Please note this is not production-ready software. See the [project status](#project-status) section for details.

---

## MCP server — `adog-mcp`

[Model Context Protocol (MCP)](https://modelcontextprotocol.io) server that gives AI assistants live access to guideline analysis.

Your AI can analyze pipeline or template YAML against current guidelines and return precise,
rule-keyed diagnostics instead of relying only on training data.

**→ See [MCP Server Reference](docs/mcp-reference.md) for installation, configuration, and usage.**

---

## What does it analyze?

The guidelines cover seven categories of Azure Pipelines YAML. Each rule has a stable ID in the
form `ADOG-{CATEGORY}-{NNN}`.

| Category | Covers |
| --- | --- |
| `GENERAL` | Pipeline-wide structural rules |
| `JOBS` | Job definition guidance |
| `PARAMETERS` | Parameter declaration and defaults |
| `PIPELINES` | Pipeline-level settings |
| `STAGES` | Stage structure and ordering |
| `STEPS` | Step and task guidelines |
| `VARIABLES` | Variable declarations and scoping |

For the full rule list and definitions, see the
[Azure Pipelines Guidelines repository](https://github.com/ruijarimba/azure-pipelines-guidelines).

## Technology at a glance

- **.NET 10 and C# 13** provide the layered application and executable MCP host.
- **Model Context Protocol (MCP)** exposes analysis tools, guideline lookups, resources, and prompts to AI clients.
- **ASP.NET Core** provides the HTTP transport; `stdio` supports clients that start the server locally.
- **Docker Hub** provides a published container for clients that support stdio servers.

## Prerequisites

| Option | Requirement |
| --- | --- |
| MCP server from Docker Hub | [Install Docker Desktop](https://docs.docker.com/get-docker/) — no .NET required |
| MCP server from a local clone | [Download the .NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) |

## Getting started

### Option 1 — MCP server from Docker Hub

No repository clone or .NET SDK is required. Configure your AI client to launch the published
container over standard input/output (Claude Desktop example):

```json
{
  "mcpServers": {
    "azure-pipelines-guidelines": {
      "command": "docker",
      "args": [
        "run",
        "-i",
        "--rm",
        "--pull",
        "always",
        "-e",
        "MCP_TRANSPORT=stdio",
        "ruijarimba/azure-pipelines-guidelines-mcp:latest"
      ]
    }
  }
}
```

### Option 2 — MCP server from a local clone

Configure your AI client to run the MCP host from an absolute repository path (Claude Desktop
example):

```json
{
  "mcpServers": {
    "azure-pipelines-guidelines": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/absolute/path/to/azure-pipelines-guidelines-ai-mcp/tools/AzurePipelines.Guidelines.Mcp.Host",
        "--"
      ]
    }
  }
}
```

**→ See [MCP Server Reference](docs/mcp-reference.md) for detailed configuration and troubleshooting.**

## Project documentation

- **[Architecture guide](docs/architecture.md)** — dependency graph, layer responsibilities, and extension points
- **[How it works](docs/how-it-works.md)** — analysis pipeline and two-repository model
- **[MCP token usage guide](docs/mcp-token-usage.md)** — how to keep client token usage low
- **[Contributing guide](CONTRIBUTING.md)** — build instructions and how to add rules

## Repository structure

```
.github/   AI agent instructions and prompt files
docs/      Architecture, decisions, glossary, and vision documents
scripts/   Local run, publish, and validation scripts
src/       Production class libraries
           Core · Parsing · Rules · Analysis · Mcp
tests/     Unit and integration test projects
tools/     MCP server executable host
           Mcp.Host (adog-mcp)
```

## Project status

This project is a **proof of concept**. It is not production-ready software.

The idea is to provide a live MCP server that can be used by AI assistants to analyze Azure Pipelines YAML against the current guidelines.
Guidelines implementation might be incomplete and/or contain bugs, and some guidelines might not be enforceable. 

## Companion repository

The rule definitions live in the
[Azure Pipelines Guidelines repository](https://github.com/ruijarimba/azure-pipelines-guidelines).
That repository owns the `data/guidelines.json` manifest and assigns rule IDs.

This repository only **implements** the tooling. It does not define or own the rules.

## License

This project is licensed under the [MIT License](LICENSE).

