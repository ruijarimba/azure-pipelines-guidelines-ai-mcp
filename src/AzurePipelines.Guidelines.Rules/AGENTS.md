# AGENTS.md — AzurePipelines.Guidelines.Rules

## Purpose

Contains one class per guideline rule. Each class implements `IRule` from `Core` and
corresponds to a specific `ADOG-{CATEGORY}-{NNN}` identifier from the companion manifest.

## What belongs here

- Implementations of `IRule` for every supported guideline.
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
- Rule classes are **stateless**; all inputs arrive through the `IRule.Analyze(…)` parameters.
- Rules must **never throw** — return an empty `IReadOnlyList<Diagnostic>` when no violations
  are found. Exceptions indicate a programming error, not an analysis result.
- Severity mapping follows the manifest strictly:
  - `do` / `do-not` → `DiagnosticSeverity.Error`
  - `avoid` → `DiagnosticSeverity.Warning`
  - `consider` → `DiagnosticSeverity.Info`
  - The lowercase manifest forms (`do-not`) map to the PascalCase `GuidelineSeverity` enum
    (`DoNot`). See [`docs/glossary.md`](../../docs/glossary.md) for both notations.
- `IRule.RuleId` must return the **exact** `ADOG-…` string from the manifest — copy it,
  do not paraphrase.

## Adding a new rule

Follow the guided workflow in `.github/prompts/implement-rule.prompt.md`.
