# azure-pipelines-guidelines-ai-mcp

A .NET 10 implementation of an **MCP server** and **CLI static analyser** for the
[azure-pipelines-guidelines](https://github.com/ruijarimba/azure-pipelines-guidelines).

## What this does

| Tool | Description |
| --- | --- |
| **MCP server** | Exposes guideline lookup and Azure Pipelines YAML analysis as [Model Context Protocol](https://modelcontextprotocol.io) tools and resources |
| **CLI (`adog`)** | Analyses Azure YAML pipeline files against the guidelines; reports violations with fix suggestions |

## Repository layout

```
src/      Class libraries (NuGet packages)
tools/    Executable entry points
tests/    Unit test projects
docs/     Architecture documentation
.github/  Copilot instructions and CI workflows
```

See [`docs/architecture.md`](docs/architecture.md) for the full design and dependency graph.

## Companion repository

The guidelines and their machine-readable manifest live in
[ruijarimba/azure-pipelines-guidelines](https://github.com/ruijarimba/azure-pipelines-guidelines).
Rule IDs follow the pattern `ADOG-{CATEGORY}-{NNN}` (e.g., `ADOG-STEPS-001`).
