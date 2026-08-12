# Azure Pipelines Guidelines MCP server

![Status: proof of concept](https://img.shields.io/badge/status-proof--of--concept-orange)

The `adog-mcp` server gives AI assistants access to the [Azure Pipelines coding guidelines](https://github.com/ruijarimba/azure-pipelines-guidelines).
It returns rule-backed diagnostics for pipelines and steps, jobs, stages, and variables templates
with stable rule IDs and fix suggestions.

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

## Prerequisites

| Option | Requirement |
| --- | --- |
| MCP server from a local clone | [Download the .NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) |
| MCP server as a Docker container | [Install Docker Desktop](https://docs.docker.com/get-docker/) — no .NET required |

## Getting started

### Option 1 — MCP server from a local clone

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

**→ See [MCP Server Reference](docs/mcp-reference.md) for configuration, available tools, and usage examples.**

### Option 2 — MCP server (Docker)

No .NET SDK is required. Start the published HTTP container with the Compose wrapper:

```powershell
pwsh ./scripts/run-mcp-compose.ps1
```

The MCP endpoint is available at `http://localhost:8080/mcp` by default. Compose does not use `.env` when running the MCP server.

Configure an HTTP-capable AI client to use the endpoint:

```json
{
  "servers": {
    "azure-pipelines-guidelines": {
      "type": "http",
      "url": "http://localhost:8080/mcp"
    }
  }
}
```

The container uses Streamable HTTP by default. If an MCP client must launch Docker as a child process over stdio, override the transport explicitly:

```powershell
docker run -i --rm --pull always -e MCP_TRANSPORT=stdio ruijarimba/azure-pipelines-guidelines-mcp:latest
```

For a hosted deployment, terminate HTTPS at your reverse proxy, ingress controller, load balancer, or managed container platform. Add authentication and authorization before exposing the endpoint outside a trusted network.

To publish a multi-architecture `latest` image to Docker Hub, copy `.env.example` to `.env`, set `DOCKERHUB_USERNAME`, set `DOCKERHUB_IMAGE` to the `username/repository` form, and set `DOCKERHUB_TOKEN` to a Docker Hub personal access token beginning with `dckr_pat_`. The publish script checks that `.env`, Docker Desktop, Docker, and Buildx are ready before it logs in or starts the build:

```powershell
pwsh ./scripts/publish-mcp-image.ps1
```

**→ See [MCP Server Reference](docs/mcp-reference.md) for detailed configuration and troubleshooting.**

## Project documentation

- **[Architecture guide](docs/architecture.md)** — dependency graph, layer responsibilities, and extension points
- **[How it works](docs/how-it-works.md)** — analysis pipeline and two-repository model
- **[MCP token usage guide](docs/mcp-token-usage.md)** — how to keep client token usage low
- **[Contributing guide](CONTRIBUTING.md)** — build instructions and how to add rules

## Repository structure

```
src/       Class libraries configured for future NuGet packages
           Core · Parsing · Rules · Analysis · Mcp
tools/     MCP server executable entry point
            Mcp.Host (adog-mcp)
tests/     Unit test projects, one per src/ library
docs/      Architecture, decisions, glossary, and vision documents
.github/   AI agent instructions and prompt files
```

## Disclaimer

This is a **proof of concept** developed in the author's spare time. It is **not production-ready**
and is provided **as-is**, with no guarantees of support, maintenance, or fitness for any particular
purpose.

**Use at your own risk.** Bugs and incomplete behavior are expected. The analyzer cannot
automatically enforce every guideline. See the [rule detection kinds and enforcement scope](docs/how-it-works.md#detection-kinds)
for the current limitations. Results should not be treated as a complete validation of an Azure
Pipelines file.

If you find issues or have suggestions, contributions are welcome, but there is no commitment to
addressing them within any particular timeframe.

## Companion repository

The rule definitions live in the
[Azure Pipelines Guidelines repository](https://github.com/ruijarimba/azure-pipelines-guidelines).
That repository owns the `data/guidelines.json` manifest and assigns rule IDs.

This repository only **implements** the tooling. It does not define or own the rules.

## License

This project is licensed under the [MIT License](LICENSE).

