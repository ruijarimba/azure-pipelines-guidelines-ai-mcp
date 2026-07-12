# AGENTS.md — AzurePipelines.Guidelines.Rules

## Purpose

Contains one class per guideline rule. Each class implements `IGuidelineRule` from `Core` and
corresponds to a specific `ADOG-{CATEGORY}-{NNN}` identifier from the companion manifest.

## What belongs here

- Implementations of `IGuidelineRule` for every supported guideline.
- Rule registration helpers — a DI extension method that registers all rules.
- Shared rule utility code (e.g., YAML AST path helpers) — kept `internal`.

## What does NOT belong here

- YAML parsing → `Parsing`
- Analyser orchestration → `Analysis`
- Anything that reads from disk or network.

## Dependencies (internal)

- `AzurePipelines.Guidelines.Core` only.
  Rules must not reference `Parsing`, `Analysis`, or `Mcp`.

## Key patterns

- **One file per rule**, named after the guideline behaviour:
  `AbsoluteTemplatePathRule.cs` (for `ADOG-GENERAL-001`).
- Rule classes are **stateless**; all inputs arrive through the `IGuidelineRule.EvaluateAsync(…)` parameters.
- `EvaluateAsync` validates arguments (`ArgumentNullException.ThrowIfNull(document)`) and honours
  cancellation (`cancellationToken.ThrowIfCancellationRequested()`); apart from those it must not
  throw for well-formed input — yield an empty sequence when no violations are found.
- Severity mapping follows the manifest strictly:
  - `do` / `do-not` → `DiagnosticSeverity.Error`
  - `avoid` → `DiagnosticSeverity.Warning`
  - `consider` → `DiagnosticSeverity.Info`
  - The lowercase manifest forms (`do-not`) map to the PascalCase `GuidelineSeverity` enum
    (`DoNot`). See [`docs/glossary.md`](../../docs/glossary.md) for both notations.
- The `GuidelineId` property must return the **exact** `ADOG-…` string from the manifest — copy it,
  do not paraphrase.

## Adding a new rule

Follow the guided workflow in `.github/prompts/implement-rule.prompt.md`.
