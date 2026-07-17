# Vision & Roadmap

## North star

Build **two tools** on top of the [Azure Pipelines Guidelines repository](https://github.com/ruijarimba/azure-pipelines-guidelines)
machine-readable manifest that make the guidelines **actionable** for humans and AI assistants:

1. **MCP server** — AI assistants call it to look up guidelines and analyse pipeline YAML.
2. **CLI static analyser** (`adog`) — runs in CI or locally; flags violations with fix suggestions.

Both tools consume the same `data/guidelines.json` manifest from the companion repository.

## In scope

```mermaid
gantt
    title Project Roadmap
    dateFormat  YYYY-MM-DD
    axisFormat  %b %Y

    section Phase 1 - Initial Release
    YAML parsing & AST                :done, p1-parse, 2024-11-01, 30d
    Rule implementations              :done, p1-rules, after p1-parse, 45d
    MCP server                        :done, p1-mcp, after p1-parse, 30d
    CLI tool (adog)                   :done, p1-cli, after p1-parse, 30d
    Output formatters                 :done, p1-fmt, after p1-cli, 14d
    Package and distribution assets   :done, p1-dist, after p1-rules, 7d
    Documentation                     :done, p1-docs, after p1-cli, 21d
    Unit test coverage (>95%)         :done, p1-tests, 2024-11-01, 90d

    section Phase 2 - Future Enhancements
    Autofixable rules                 :p2-autofix, 2025-02-01, 30d
    IDE extensions                    :p2-ide, after p2-autofix, 45d
    CI/CD integrations                :p2-cicd, after p2-ide, 30d
    LLM-assisted heuristic rules      :p2-llm, after p2-cicd, 60d
    Automatic manifest updates        :p2-manifest, after p2-llm, 21d
```

### Phase 1 (initial release) ✅

- Parse Azure Pipelines YAML into a structured AST.
- Implement rules for all `ADOG-{CATEGORY}-{NNN}` guidelines in the manifest.
- MCP server (tools: guideline lookup, YAML analysis, fix suggestions; resources: guideline catalogue).
- CLI tool (`adog analyze`, `adog rules list`, `adog rules show`).
- Console, compact, JSON, JUnit, SARIF, and Markdown output formats.
- JSON configuration-file defaults for CLI options.
- NuGet package metadata and local packing for all `src/` libraries.
- Global-tool packaging configuration for `adog` and `adog-mcp`.
- Docker-image distribution assets and Docker Hub publication for `adog-mcp`.
- NuGet publication is deferred. The package configuration remains for a future release.
- Comprehensive unit test coverage (xUnit + FluentAssertions + NSubstitute) with repository-wide line coverage strictly above 95% and explicit tests for success, failure, and edge-case scenarios.

### Phase 2 (future enhancements) 🔮

- Autofixable rules (deterministic text transformations).
- IDE extensions (VS Code, Visual Studio) using the analysis engine.
- CI/CD integrations (Azure Pipelines task, GitHub Action).
- LLM-assisted analysis for `heuristic` detection rules.
- Manifest updates: consume new rules from the companion repository automatically.

## Out of scope

### Permanently out of scope

- Authoring the guidelines — they live in the companion repository.
- Linting for GitHub Actions, GitLab CI, Jenkins, or other CI/CD systems.
- Runtime analysis or monitoring — this is a static analyser only.
- Pipeline execution simulation or validation.

### Deferred to Phase 2 or later

- Auto-fixing beyond deterministic text replacements.
- Telemetry or usage analytics.
- Custom user-defined rules.

## Success criteria

### Phase 1 complete when

- All `ADOG-…` rules from `guidelines.json` are implemented.
- MCP server responds correctly to all defined tools and resources.
- CLI produces accurate diagnostics and exits with correct codes.
- Package metadata and local packing remain valid for a future NuGet release.
- Docker image distribution remains available for the MCP server.
- Repository-wide line coverage strictly above 95% (enforced by `scripts/quality-check.ps1`).
- Tests must cover normal success paths, failure paths, and edge cases for every behavior change; no change is accepted without broad regression coverage.
- Documentation complete: `AGENTS.md`, `architecture.md`, this file, and per-project `AGENTS.md`.

### Long-term success

- Community adoption: ≥ 100 downloads/week for `adog` global tool within 6 months.
- Integration: used in at least one production CI pipeline.
- Contribution: external contributor submits a rule implementation or bug fix.
