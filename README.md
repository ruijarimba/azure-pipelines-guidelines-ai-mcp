# azure-pipelines-guidelines-ai-mcp

This project provides tools that help you follow Azure Pipelines best practices. It offers both a static analyser you can run in CI/CD and an AI assistant integration.

## What it does

| Tool | Description |
| --- | --- |
| **MCP server** | Exposes guideline lookup and Azure Pipelines YAML analysis as [Model Context Protocol](https://modelcontextprotocol.io) tools and resources |
| **CLI (`adog`)** | Analyses Azure YAML pipeline files against the guidelines; reports violations with fix suggestions |

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
