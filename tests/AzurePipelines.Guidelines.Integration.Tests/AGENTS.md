# AGENTS.md — AzurePipelines.Guidelines.Integration.Tests

## Purpose

In-process integration tests for committed, multi-file Azure Pipelines fixture repositories.

## Boundaries

- Run the real parser, path resolver, registered rules, and analyser through DI; never invoke the CLI from a test.
- Keep compact, purpose-built fixtures under `Fixtures/PipelineRepositories`. Each repository must contain multiple readable YAML files and repeated violations.
- Maintain one derived test class per fixture repository (`DockerPipelineRepositoryTests`, `HelmPipelineRepositoryTests`, and `TerraformPipelineRepositoryTests`) over `PipelineRepositoryIntegrationTestsBase`.
- Each repository class owns its expected guideline IDs and uses analysis filtering to avoid brittle assertions caused by intentionally overlapping rule triggers.
- Keep the three expected-ID sets balanced (roughly 15–20 IDs each) and ensure their union covers every registered rule. `RegisteredGuidelineCoverageTests` enforces this contract.
- Do not call external services or load the remote guideline catalogue.
- Use rule-ID-based expectations so diagnostics remain stable as wording evolves.

## Manual debugging

Run the test project with `dotnet test tests/AzurePipelines.Guidelines.Integration.Tests`.
Use `scripts/smoke-integration-fixtures.ps1` only for manual fixture diagnostics, not as part of test execution.
