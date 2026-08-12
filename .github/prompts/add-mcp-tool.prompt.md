---
mode: agent
---

# Add a new MCP tool handler

Use this prompt when exposing a new capability via the Model Context Protocol server.
This checklist keeps the handler, registration, tests, and transport constraints aligned with the repository standards.

## Inputs required

Before starting, confirm the following:

1. **Tool name** — the MCP tool name as it will appear to AI clients (e.g., `analyze_template`).
2. **Description** — one-sentence description shown to the AI client.
3. **Input parameters** — name, type, and description of each parameter.
4. **Return value** — shape of the response (text, structured JSON, etc.).
5. **Dependencies** — which `Core` interfaces the handler needs (`IAnalysisEngine`, `IGuidelineRepository`, etc.).

## Steps

1. **Create the tool handler class** in `src/AzurePipelines.Guidelines.Mcp/Tools/`.
   - Annotate the method with `[McpServerTool]` and `[Description("...")]` from the MCP SDK:
     ```csharp
     [McpServerTool, Description("One-sentence description shown to the AI client.")]
     public async Task<string> ToolNameAsync(
         [Description("Parameter description.")] string paramName,
         CancellationToken cancellationToken = default)
     ```
   - Inject dependencies via constructor (all dependencies are `Core` interfaces).
   - Keep the class `internal sealed`; register it via the DI extension method.

2. **Register the handler** in the `AddGuidelinesMcp(…)` extension method.

3. **Create the test class** in `tests/AzurePipelines.Guidelines.Mcp.Tests/`.
   - Name: `{ToolName}ToolTests`.
   - Test: valid inputs → correct response shape.
   - Test: invalid inputs → appropriate error response.
   - Test: edge cases (empty YAML, unknown guideline IDs, null parameters).
   - Substitute all `Core` interface dependencies via NSubstitute.

4. **Run tests** and confirm all pass before committing.
   - Keep the MCP transport contract intact: do not write extra output to `stdout`; use `stderr` for logs and diagnostics.
