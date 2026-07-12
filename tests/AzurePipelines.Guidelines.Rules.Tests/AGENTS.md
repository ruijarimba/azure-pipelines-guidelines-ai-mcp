# AGENTS.md — AzurePipelines.Guidelines.Rules.Tests

## Purpose

Unit tests for `AzurePipelines.Guidelines.Rules` — individual `IGuidelineRule` implementations.

## What gets tested here

Each rule class gets its own test class: `{RuleName}Tests`.

For every rule:

- **Compliant document** → no diagnostics returned.
- **Violating document** → diagnostics with correct `GuidelineId`, severity, message, location.
- **Edge cases**: null nodes, missing keys, empty collections, boundary values.

## Test naming

`RuleName_GivenCompliantDocument_ShouldReturnNoDiagnostics`
`RuleName_GivenViolation_ShouldReturnDiagnosticWithCorrectGuidelineId`
`RuleName_GivenNullInput_ShouldReturnNoDiagnostics`

## Coverage expectations

- Every detection branch in the rule.
- All severity mappings verified (`do`/`do-not` → Error, `avoid` → Warning, `consider` → Info).
- Argument validation and cancellation are honoured — tests must prove defensive null handling.

## Test data

Use in-memory `PipelineDocument` instances built via object initializers.
For complex ASTs, consider a fluent builder or helper methods in a shared test utilities class.
