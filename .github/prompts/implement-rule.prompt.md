---
agent: agent
---

# Implement a new `IGuidelineRule`

Use this prompt when adding a rule that maps to a specific `ADOG-{CATEGORY}-{NNN}` guideline. This checklist keeps the rule implementation, registration, tests, and diagnostics aligned with the repository conventions.

## Inputs required

Before starting, confirm the following:

1. **Rule ID** — the exact `ADOG-…` identifier from `data/guidelines.json` in the companion repository.
2. **Guideline summary** — the one-line summary from the manifest.
3. **Detection hint** — the `detection` array entries (kind, pattern, appliesTo, message).
4. **Severity** — `do`/`do-not` → Error, `avoid` → Warning, `consider` → Info.
5. **Autofixable** — whether the `fix.autofixable` flag is `true`.

## Steps

1. **Create the rule class** in `src/AzurePipelines.Guidelines.Rules/`.
   - Name: `{DescriptiveName}Rule` (derive the name from the guideline title).
   - Implement `IGuidelineRule` from `AzurePipelines.Guidelines.Core`.
   - Follow the exact class structure in `.github/instructions/csharp-patterns.instructions.md` §1.
   - `GuidelineId` property returns the exact `ADOG-…` identifier declared as `private static readonly`.
   - `EvaluateAsync` must validate its arguments (`ArgumentNullException.ThrowIfNull(document)`) and honour cancellation (`cancellationToken.ThrowIfCancellationRequested()`); apart from those, it must not throw for well-formed input. Yield no results when no violations are found.
   - Map the manifest severity to a `DiagnosticSeverity` on the produced `Diagnostic` (`do`/`do-not` → `DiagnosticSeverity.Error`, `avoid` → `DiagnosticSeverity.Warning`, `consider` → `DiagnosticSeverity.Info`). Use `GuidelineSeverityExtensions.ToDiagnosticSeverity` where a `GuidelineSeverity` is available.

2. **Register the rule** via the DI extension method in `Rules` (or add a new one).

3. **Create the test class** in `tests/AzurePipelines.Guidelines.Rules.Tests/`.
   - Name: `{DescriptiveName}RuleTests`.
   - Test: compliant document → no diagnostics.
   - Test: violating document → diagnostics with correct `GuidelineId` and severity.
   - Test: edge cases (null nodes, missing keys, empty YAML sections).

4. **Verify** that the `Diagnostic.GuidelineId` matches the manifest exactly (copy-paste from `guidelines.json`).

5. **Run tests** and confirm all pass before committing.
