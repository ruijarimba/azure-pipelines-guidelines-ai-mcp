# AGENTS.md — AzurePipelines.Guidelines.Mcp.Tests

## Purpose

Unit tests for `AzurePipelines.Guidelines.Mcp` — MCP tool and resource handlers.

## Folder layout

| Folder | Tests for |
| --- | --- |
| `Tools/` | MCP tool handlers (`GuidelineTools`, `PipelineAnalysisTools`) |
| `Resources/` | MCP resource handlers (`GuidelineResources`) |

## What gets tested here

Each MCP tool handler gets its own test class in `Tools/`: `{HandlerName}Tests.cs`.
Each MCP resource handler gets its own test class in `Resources/`: `{HandlerName}Tests.cs`.

For every handler:

- **Valid inputs** → correct response shape and content.
- **Invalid inputs** → appropriate error response (not exceptions).
- **Edge cases**: null parameters, empty YAML strings, unknown rule IDs, missing files.

## Test naming

`ListGuidelines_GivenEmptyRepository_ShouldReturnEmptyArray`
`GetGuidelineAsync_GivenUnknownId_ShouldReturnErrorResponse`
`AnalyzeTemplateAsync_GivenValidYaml_ShouldReturnDiagnostics`

## Coverage expectations

- All handler method branches (success, validation failures, business logic errors).
- Response schema conformance (the AI client expects a specific shape).
- Dependency injection: correct `IPipelineParser` / `IPipelineAnalyser` / `IGuidelineRepository` usage.

## Test doubles

- Substitute `IPipelineParser`, `IPipelineAnalyser`, `IGuidelineRepository` via NSubstitute.
- Never test the MCP protocol transport layer — that is the SDK's job.
  Focus on handler logic only.
