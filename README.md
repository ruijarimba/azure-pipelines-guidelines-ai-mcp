# Azure Pipelines Guidelines MCP server

![Status: proof of concept](https://img.shields.io/badge/status-proof--of--concept-orange)

**TL;DR:** This is a proof-of-concept MCP server that AI assistants can use to analyze Azure Pipelines YAML against the [Azure Pipelines coding guidelines](https://github.com/ruijarimba/azure-pipelines-guidelines).

This is a **static, deterministic MCP server**. It parses Azure Pipelines YAML, loads the guideline manifest, applies the implemented rules, and returns structured diagnostics. It does not use retrieval-augmented generation (RAG), vector search, an LLM, or model-generated analysis.

The connected AI client may explain these diagnostics in natural language, but that explanation does not come from this server. The following example shows how VS Code Copilot presents results from this server:

![VS Code Copilot displays an Azure Pipelines violation summary table with severity, rule, finding, evidence, and recommendation columns.](/docs/images/vscode-copilot-violations-summary.png)

## Project status

This project is a **proof of concept**. It is not production-ready software.

Guideline implementation may be incomplete or contain bugs, and some guidelines may not be enforceable.

## What does it analyze?

The server analyzes Azure Pipelines YAML files against guidelines organized into seven categories. Each guideline has a stable ID in the form `ADOG-{CATEGORY}-{NNN}`:

| Category | Covers |
| --- | --- |
| [`GENERAL`](https://github.com/ruijarimba/azure-pipelines-guidelines/tree/main/guidelines/general) | Pipeline-wide structural rules |
| [`JOBS`](https://github.com/ruijarimba/azure-pipelines-guidelines/tree/main/guidelines/jobs) | Job definition guidance |
| [`PARAMETERS`](https://github.com/ruijarimba/azure-pipelines-guidelines/tree/main/guidelines/parameters) | Parameter declaration and defaults |
| [`PIPELINES`](https://github.com/ruijarimba/azure-pipelines-guidelines/tree/main/guidelines/pipelines) | Pipeline-level settings |
| [`STAGES`](https://github.com/ruijarimba/azure-pipelines-guidelines/tree/main/guidelines/stages) | Stage structure and ordering |
| [`STEPS`](https://github.com/ruijarimba/azure-pipelines-guidelines/tree/main/guidelines/steps) | Step and task guidelines |
| [`VARIABLES`](https://github.com/ruijarimba/azure-pipelines-guidelines/tree/main/guidelines/variables) | Variable declarations and scoping |

The [Azure Pipelines Guidelines repository](https://github.com/ruijarimba/azure-pipelines-guidelines) owns the manifest and metadata and defines the rules and guidelines.

## Technology at a glance

- **.NET 10 and C# 13** provide the layered application and executable MCP host.
- **Model Context Protocol (MCP)** exposes analysis tools, guideline lookups, resources, and prompts to AI clients. Built using the [official C# MCP SDK](https://github.com/modelcontextprotocol/csharp-sdk).
- **ASP.NET Core** provides the HTTP transport; `stdio` supports clients that start the server locally.
- **Docker Hub** hosts the published MCP container image for clients that launch it in `stdio` mode or connect via HTTP.

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
        "--mount",
        "type=bind,source=${workspaceFolder},target=/workspace,readonly",
        "--workdir",
        "/workspace",
        "-e",
        "MCP_TRANSPORT=stdio",
        "ruijarimba/azure-pipelines-guidelines-mcp:latest"
      ]
    }
  }
}
```

`${workspaceFolder}` is expanded by VS Code to the currently open workspace, so this configuration
can be reused across repositories. The workspace is mounted read-only so the server can analyze
pipeline files without modifying them; Copilot can still suggest and apply fixes through the editor.

### Option 2 — MCP server from a local clone

Create or edit `.vscode/mcp.json` in your project to run the MCP host from an absolute repository path:

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

## License

This project is licensed under the [MIT License](LICENSE).
