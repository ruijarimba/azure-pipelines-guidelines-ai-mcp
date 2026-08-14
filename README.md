# Azure Pipelines Guidelines MCP server

![Status: proof of concept](https://img.shields.io/badge/status-proof--of--concept-orange)

**TL;DR:** This is a proof-of-concept MCP server that AI assistants can use to analyze Azure Pipelines YAML against the [Azure Pipelines coding guidelines](https://github.com/ruijarimba/azure-pipelines-guidelines).

It returns rule-backed diagnostics for pipelines and templates covering steps, jobs, stages, and variables, with stable rule IDs and fix suggestions.

Example output from the VS Code Copilot extension after this MCP server analyzes Azure Pipelines files:

![VS Code Copilot displays an Azure Pipelines violation summary table with severity, rule, finding, evidence, and recommendation columns.](/docs/images/vscode-copilot-violations-summary.png)

## Project status

This project is a **proof of concept**. It is not production-ready software.

Guideline implementation may be incomplete or contain bugs, and some guidelines may not be enforceable.



## What does it analyze?

The server analyzes Azure Pipelines YAML files against guidelines organized into seven categories. Each guideline has a stable ID in the form `ADOG-{CATEGORY}-{NNN}`:

| Category | Covers |
| --- | --- |
| `GENERAL` | Pipeline-wide structural rules |
| `JOBS` | Job definition guidance |
| `PARAMETERS` | Parameter declaration and defaults |
| `PIPELINES` | Pipeline-level settings |
| `STAGES` | Stage structure and ordering |
| `STEPS` | Step and task guidelines |
| `VARIABLES` | Variable declarations and scoping |

The [Azure Pipelines Guidelines repository](https://github.com/ruijarimba/azure-pipelines-guidelines) owns the manifest and metadata and defines the rules and guidelines.

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

For more options and details regarding installation, configuration, and usage, see the [MCP Server Reference](docs/mcp-reference.md).

### Option 1 — MCP server from Docker Hub

No repository clone or .NET SDK is required. Create or edit `.vscode/mcp.json` in your project:

```json
{
  "servers": {
    "azure-pipelines-guidelines": {
      "type": "stdio",
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

Create or edit `.vscode/mcp.json` in your project to run the MCP host from an absolute repository
path:

```json
{
  "servers": {
    "azure-pipelines-guidelines": {
      "type": "stdio",
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

## Example prompts

Once the MCP server is connected, try:

- “Review this Azure Pipelines YAML for guideline violations and suggest fixes.”
- “Analyze all pipeline and template files in this workspace and summarize findings by rule.”
- “Explain guideline `ADOG-STEPS-001` and show how to fix the violation.”
- “List the Azure Pipelines guidelines related to variables and summarize the most relevant ones.”

## Project documentation

- **[Architecture guide](docs/architecture.md)** — dependency graph, layer responsibilities, and extension points
- **[How it works](docs/how-it-works.md)** — analysis pipeline and two-repository model
- **[MCP Server Reference](docs/mcp-reference.md)** — installation, configuration, tools, resources, prompts, and troubleshooting
- **[MCP token usage guide](docs/mcp-token-usage.md)** — how to keep client token usage low
- **[Glossary](docs/glossary.md)** — project and MCP terminology
- **[Architecture decisions](docs/decisions.md)** — important design decisions and their rationale
- **[Project vision](docs/vision.md)** — project goals and planned direction
- **[Contributing guide](CONTRIBUTING.md)** — build instructions and how to add rules

## License

This project is licensed under the [MIT License](LICENSE).

