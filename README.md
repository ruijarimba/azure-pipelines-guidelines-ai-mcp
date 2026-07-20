# Azure Pipelines Guidelines tools

Command-line and AI assistant tools for checking Azure Pipelines YAML against the [Azure Pipelines coding guidelines](https://github.com/ruijarimba/azure-pipelines-guidelines). Get rule-backed diagnostics for your pipeline files with stable rule IDs and fix suggestions — enforce coding guidelines across your team and catch issues early.

**Two tools:**

- **CLI (`adog`)** — static analyzer for local development and CI/CD pipelines
- **MCP server (`adog-mcp`)** — AI assistant integration (GitHub Copilot, Claude, Cursor, etc.)

---

## CLI — `adog`

Command-line static analyzer that checks pipeline files for violations and reports issues with fix suggestions.

Run it locally during development or integrate it into your CI/CD pipeline to enforce coding guidelines.

**→ See [CLI Reference](docs/cli-reference.md) for installation, commands, output formats, and examples.**

## MCP Server — `adog-mcp`

[Model Context Protocol (MCP)](https://modelcontextprotocol.io) server that gives AI assistants live access to guideline analysis.

Your AI can analyze pipeline YAML against current guidelines and return precise, rule-keyed diagnostics instead of relying only on training data.

**→ See [MCP Server Reference](docs/mcp-reference.md) for installation, configuration, and usage.**

### Sample prompts

Ask your AI client:

> Analyze this Azure Pipelines YAML against the available guidelines. Identify any issues, explain
> what each issue means, and suggest a concise fix. Do not modify the file.

Or:

> Analyze all Azure Pipelines YAML files under `<directory>`. Summarize the findings and point out
> repeated issues across files. Do not modify the files.

See the [MCP sample prompts](docs/mcp-prompts.md) for more examples, including template reviews,
rule exclusions, category filters, and low-token analysis.

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
For local analyzer coverage and the reason each rule is enforceable, heuristic, or not automatable,
see the [guideline automation status](docs/guideline-automation.md).

## Prerequisites

| Option | Requirement |
| --- | --- |
| CLI or MCP server from a local clone | [Download the .NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) |
| MCP server as a Docker container | [Install Docker Desktop](https://docs.docker.com/get-docker/) — no .NET required |

## Getting started

### Option 1 — CLI from a local clone

Clone the repository, then run the static analyzer from its root directory:

```bash
dotnet run --project tools/AzurePipelines.Guidelines.Cli -- analyze azure-pipelines.yml
```

Example output:

```
azure-pipelines.yml(12,17): warning ADOG-STEPS-001: Steps template reads a pipeline variable with $(DEPLOY_ENV). Pass values as parameters instead.
```

**→ See [CLI Reference](docs/cli-reference.md) for all commands, options, output formats, and examples.**

### Option 2 — MCP Server from a local clone

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

## Project documentation

- **[Azure Pipelines YAML schema guide](docs/azure-pipelines-schema.md)** — official schema reference,
  local validation scope, and Microsoft Learn MCP guidance
- **[Architecture guide](docs/architecture.md)** — dependency graph, layer responsibilities, and extension points
- **[How it works](docs/how-it-works.md)** — analysis pipeline and two-repository model
- **[Guideline automation status](docs/guideline-automation.md)** — local enforcement status and rationale for every implemented rule
- **[Contributing guide](CONTRIBUTING.md)** — build instructions and how to add rules

## Repository structure

```
src/       Class libraries configured for future NuGet packages
           Core · Parsing · Rules · Analysis · Mcp
tools/     Executable entry points
           Cli (adog) · Mcp.Host (adog-mcp)
tests/     Unit test projects, one per src/ library
docs/      Architecture, decisions, glossary, and vision documents
.github/   AI agent instructions and prompt files
```

## Disclaimer

This is a **proof-of-concept project** developed in the author's spare time. It is **not production-ready** and is provided **as-is** with no guarantees of support, maintenance, or fitness for any particular purpose.

**Use at your own risk.** Do not expect frequent updates or bug fixes. The project is experimental and may contain bugs, breaking changes, or incomplete features.

If you find issues or have suggestions, contributions are welcome — but there is no commitment to addressing them in any particular timeframe.

## Companion repository

The rule definitions live in the
[Azure Pipelines Guidelines repository](https://github.com/ruijarimba/azure-pipelines-guidelines).
That repository owns the `data/guidelines.json` manifest and assigns rule IDs.

This repository only **implements** the tooling. It does not define or own the rules.

## License

This project is licensed under the [MIT License](LICENSE).

