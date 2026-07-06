# AGENTS.md — AzurePipelines.Guidelines.Mcp.Tests

## Purpose

Unit tests for `AzurePipelines.Guidelines.Mcp` — MCP tool and resource handlers.

## What gets tested here

Each MCP tool handler gets its own test class: `{ToolName}ToolTests`.

For every tool:

- **Valid inputs** → correct response shape and content.
- **Invalid inputs** → appropriate error response (not exceptions).
- **Edge cases**: null parameters, empty YAML strings, unknown rule IDs, missing files.

## Test naming

`AnalyzePipelineTool_GivenValidYaml_ShouldReturnDiagnostics`
`GetGuidelineTool_GivenUnknownRuleId_ShouldReturnNotFoundResponse`
`ListRulesTool_GivenCategoryFilter_ShouldReturnMatchingRules`

## Coverage expectations

- All tool method branches (success, validation failures, business logic errors).
- Response schema conformance (the AI client expects a specific shape).
- Dependency injection: correct `IAnalysisEngine` / `IGuidelineRepository` usage.

## Test doubles

- Substitute `IAnalysisEngine`, `IGuidelineRepository` via NSubstitute.
- Never test the MCP protocol transport layer — that's the SDK's job.
  Focus on handler logic only.
