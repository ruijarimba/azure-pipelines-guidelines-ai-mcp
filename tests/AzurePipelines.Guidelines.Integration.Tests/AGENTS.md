# AGENTS.md — AzurePipelines.Guidelines.Integration.Tests

## Purpose

End-to-end integration tests for committed multi-file Azure Pipelines fixture repositories.

## Boundaries

- Run the real parser, path resolver, registered rules, and analyser through DI.
- Keep fixtures committed, readable, and independent of `.temp`.
- Do not call external services or load the remote guideline catalogue.
- Use rule-ID-based expectations so diagnostics remain stable as wording evolves.

## Manual debugging

Run the test project with `dotnet test tests/AzurePipelines.Guidelines.Integration.Tests`.
Use `scripts/smoke-integration-fixtures.ps1` to generate inspectable CLI JSON reports.
