# AGENTS.md — AzurePipelines.Guidelines.Mcp

## Purpose

Implements the Model Context Protocol (MCP) server logic. Exposes guideline lookup and Azure Pipelines YAML analysis as MCP **tools** and **resources** that AI assistants can call.

## What belongs here

- MCP tool handler implementations (guideline lookup and YAML analysis).
- MCP resource definitions (guideline catalogue, per-rule metadata).
- DI extension method: `AddGuidelinesMcp(IServiceCollection)`.
- Request/response DTOs specific to the MCP surface — these are `internal` and must not
  bleed into the domain model in `Core`.
- Internal enums, lookups, and exceptions used by the MCP layer.

## What does NOT belong here

- Business logic → `Core` / `Analysis`
- Host / process lifecycle → `Mcp.Host`
- Rule implementations → `Rules`
- Direct YAML parsing — use `IAnalysisEngine` from `Analysis`

## Dependencies (internal)

- `AzurePipelines.Guidelines.Core`
- `AzurePipelines.Guidelines.Analysis`

## Dependencies (NuGet)

- [`ModelContextProtocol`](https://github.com/modelcontextprotocol/csharp-sdk) — Official C# MCP SDK
- `Microsoft.Extensions.Hosting.Abstractions`
- `Microsoft.Extensions.Logging.Abstractions`

## Key patterns

- Each MCP tool is a separate `internal sealed` class in `Tools/`, annotated with
  `[McpServerToolType]`. Methods are annotated with `[McpServerTool(Name = "…")]`.
- Each MCP resource type is a separate `internal sealed` class in `Resources/`,
  annotated with `[McpServerResourceType]`. Methods are annotated with
  `[McpServerResource(UriTemplate = "…", Name = "…", MimeType = "…")]`.
- Both tools and resources depend on `IGuidelineRepository` (and/or `IPipelineParser` /
  `IPipelineAnalyser`) via constructor injection.
- Only the DI extension method (`AddGuidelinesMcp`) is `public`; all handler classes
  are `internal`.
- Handler classes are suppressed for CA1812 ("Avoid uninstantiated internal classes") because
  the MCP SDK creates them through dependency injection at runtime.
- Tool and resource descriptions shown to AI clients must be concise, accurate, and
  derived from the guideline manifest vocabulary.
- Handler return values are JSON strings. The shared `_jsonOptions` use camel-case property
  names and skip null values to keep responses small and predictable for clients.
- Enum values returned to clients are lower-case ASCII strings (`general`, `error`, etc.)
  produced by `EnumToJsonString<T>` to avoid globalization analyser warnings.

## Adding a new tool

Follow the guided workflow in `.github/prompts/add-mcp-tool.prompt.md`.

## Adding a new resource

1. Create a new `internal sealed` class in `Resources/` annotated with
   `[McpServerResourceType]`.
2. Suppress CA1812 with the standard justification comment (SDK instantiates the class).
3. Annotate each handler method with `[McpServerResource(UriTemplate = …)]` and
   `[Description(…)]`.
4. Use `Task<string>` as the return type; return serialised JSON.
5. `WithResourcesFromAssembly(…)` in `GuidelinesMcpServiceCollectionExtensions.cs`
   already discovers all `[McpServerResourceType]` classes — no registration change needed.
