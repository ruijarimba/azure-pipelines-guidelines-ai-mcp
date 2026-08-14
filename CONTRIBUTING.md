# Contributing

## Prerequisites

| Tool | Version | Notes |
| --- | --- | --- |
| [Download the .NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | 10.0 | Required for all tasks |
| [Install Docker Desktop](https://docs.docker.com/get-docker/) | Any recent | Required to build the Docker image only |

## Build

```bash
dotnet build
```

## Run tests

```bash
dotnet test
```

To collect code coverage:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Publish the MCP container

To publish the multi-architecture image to Docker Hub, copy `.env.example` to `.env`, set the Docker
Hub values, and run `pwsh ./scripts/publish-mcp-image.ps1`. The script pushes the `latest` tag.

TLS, authentication, and authorization belong at the reverse proxy, ingress controller, load
balancer, or managed container platform when the container is hosted outside a trusted network.

## Add a new rule

Each rule maps to one `ADOG-{CATEGORY}-{NNN}` guideline from the
[companion guidelines manifest](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/data/guidelines.json).

Before you start, check the manifest to find the rule's detection kind (`regex`, `yamlPath`, or
`heuristic`). Only `regex` and `yamlPath` rules are in scope for Phase 1 — see
[ADR-013 in the architecture decisions record](docs/decisions.md) for the rationale.

Follow the step-by-step guide in
[the rule implementation prompt](.github/prompts/implement-rule.prompt.md).

In brief:

1. Create a class in `src/AzurePipelines.Guidelines.Rules/` that implements `IGuidelineRule`.
2. Register it in the DI extension method in the same project.
3. Add tests in `tests/AzurePipelines.Guidelines.Rules.Tests/`.
4. Verify the rule ID matches the manifest exactly.

## Add a new MCP tool

Follow the guide in
[the MCP tool prompt](.github/prompts/add-mcp-tool.prompt.md).

## Code quality

Every pull request must:

- Build with zero warnings (`TreatWarningsAsErrors = true`)
- Pass all existing tests
- Include tests for any new logic, including edge cases and error paths
- Follow the patterns documented in [the repository documentation instructions](.github/instructions/)

## Architecture

The dependency graph is strict — no cycles, no upward references. Before adding a project
reference, read [the architecture guide](docs/architecture.md).

## AI-assisted development

This project is set up for AI coding agents.

- Agents and contributors must read `.github/copilot-instructions.md` and its linked instruction
  files before making changes. Whether an AI client loads them automatically depends on that
  client's configuration.
- The behaviour rules in
  [`.github/instructions/agent-behaviour.instructions.md`](.github/instructions/agent-behaviour.instructions.md)
  apply to AI agents and human contributors equally.
- Review every change an agent proposes before accepting it.
