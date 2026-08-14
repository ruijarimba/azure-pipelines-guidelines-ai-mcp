# AGENTS.md — AzurePipelines.Guidelines.Analysis.Tests

## Purpose

Unit tests for `AzurePipelines.Guidelines.Analysis` — the orchestration layer.

## What gets tested here

- **`IAnalysisEngine`** orchestrates parsing + rule execution correctly.
- **Rule filtering** by category, severity, `appliesTo` scope.
- **Aggregation** of diagnostics from multiple rules into a single `AnalysisResult`.
- **Error handling**: invalid YAML, parsing failures, exceptions during rule execution.

## Test naming

- `AnalysisEngine_GivenValidYaml_ShouldReturnDiagnostics`
- `AnalysisEngine_GivenFilterBySeverity_ShouldReturnOnlyMatchingDiagnostics`
- `AnalysisEngine_GivenParsingFailure_ShouldReturnErrorResult`

## Coverage expectations

- All filtering logic branches (category, severity, scope).
- Error paths: parsing exceptions, rule exceptions, null inputs.
- Integration: parser + rules wired through DI produce correct end-to-end behaviour.

## Test doubles

- Substitute `IPipelineParser` and `IEnumerable<IGuidelineRule>` via NSubstitute.
- Use minimal stubs — only mock the behaviour under test, not the entire call chain.

## No end-to-end tests here

This tests the **orchestration**. Full end-to-end integration tests (real YAML files → real rules → real diagnostics) belong in a separate integration test project if needed.
