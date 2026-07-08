# Azure Pipelines Guidelines tools

Two tools for analyzing Azure Pipelines YAML against the [Azure Pipelines coding guidelines published at https://github.com/ruijarimba/azure-pipelines-guidelines](https://github.com/ruijarimba/azure-pipelines-guidelines):

- **CLI (`adog`)** — static analyzer for local development and CI/CD pipelines
- **MCP server (`adog-mcp`)** — AI assistant integration for real-time guideline analysis

## Tools

### CLI — `adog`

Command-line static analyzer that checks pipeline files for violations and reports issues with fix suggestions.

Run it locally during development or integrate it into your CI/CD pipeline to enforce coding guidelines.

**→ See [CLI Reference](docs/cli-reference.md) for installation, commands, output formats, and examples.**

### MCP Server — `adog-mcp`

[Model Context Protocol (MCP)](https://modelcontextprotocol.io) server that gives AI assistants (GitHub Copilot, Claude, Cursor, etc.) live access to guideline analysis.

Your AI can analyze pipeline YAML against current guidelines and return precise, rule-keyed diagnostics instead of relying only on training data.

**→ See [MCP Server Reference](docs/mcp-reference.md) for installation, configuration, and usage.**

## Why use it?

Use these tools when you want to:

- Check Azure Pipelines YAML locally or in CI with rule-backed diagnostics
- Give an AI assistant live access to Azure Pipelines guidance through MCP
- Review violations with stable rule IDs and fix suggestions
- Enforce coding guidelines across your team

Azure Pipelines is Microsoft's CI/CD platform. Writing correct, consistent pipelines is hard — especially on teams that want a shared, reviewable reference for pipeline authoring.

The [Azure Pipelines Guidelines repository](https://github.com/ruijarimba/azure-pipelines-guidelines) defines the coding guidelines as a machine-readable manifest. This repository **implements the tooling** that makes those guidelines actionable.

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

## Prerequisites

| Option | Requirement |
| --- | --- |
| CLI or MCP server as a global tool | [Download the .NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) |
| MCP server as a Docker container | [Install Docker Desktop](https://docs.docker.com/get-docker/) — no .NET required |

## Getting started

### Option 1 — CLI

Install and run the static analyzer:

```bash
dotnet tool install -g adog
adog analyze azure-pipelines.yml
```

Example output:

```
azure-pipelines.yml(12,17): warning ADOG-STEPS-001: Steps template reads a pipeline variable with $(DEPLOY_ENV). Pass values as parameters instead.
```

**→ See [CLI Reference](docs/cli-reference.md) for all commands, options, output formats, and examples.**

### Option 2 — MCP Server (global tool)

Install the MCP server and configure your AI client:

```bash
dotnet tool install -g adog-mcp
```

Add to your AI client config (Claude Desktop example):

```json
{
  "mcpServers": {
    "azure-pipelines-guidelines": {
      "command": "adog-mcp"
    }
  }
}
```

**→ See [MCP Server Reference](docs/mcp-reference.md) for configuration, available tools, and usage examples.**

### Option 3 — MCP Server (Docker)

No .NET SDK required — use the Docker image:

```bash
docker pull ruijarimba/azure-pipelines-guidelines-mcp:latest
```

Configure your AI client to use Docker:

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

**→ See [MCP Server Reference](docs/mcp-reference.md) for detailed configuration and troubleshooting.**

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

## Prerequisites

| Option | Requirement |
| --- | --- |
| CLI or MCP server as a global tool | [Download the .NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) |
| MCP server as a Docker container | [Install Docker Desktop](https://docs.docker.com/get-docker/) — no .NET required |

## Project documentation

- **[Architecture guide](docs/architecture.md)** — dependency graph, layer responsibilities, and extension points
- **[How it works](docs/how-it-works.md)** — analysis pipeline and two-repository model
- **[Contributing guide](CONTRIBUTING.md)** — build instructions and how to add rules

## Repository structure

```
src/       Class libraries published as NuGet packages
           Core · Parsing · Rules · Analysis · Mcp
tools/     Executable entry points
           Cli (adog) · Mcp.Host (adog-mcp)
tests/     Unit test projects, one per src/ library
docs/      Architecture, decisions, glossary, and vision documents
.github/   AI agent instructions, prompt files, and CI workflows
```

## Companion repository

The rule definitions live in the
[Azure Pipelines Guidelines repository](https://github.com/ruijarimba/azure-pipelines-guidelines).
That repository owns the `data/guidelines.json` manifest and assigns rule IDs.

This repository only **implements** the tooling. It does not define or own the rules.
