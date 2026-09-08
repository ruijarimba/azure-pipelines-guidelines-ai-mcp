---
applyTo: "**/*.cs"
---

# C# code style

## Language features to use

- **File-scoped namespaces** — `namespace Foo.Bar;`, never block-scoped `namespace Foo.Bar { }`.
- **Primary constructors** for classes whose constructor body only assigns fields.
- **Records** for all immutable domain models (`GuidelineDefinition`, `Diagnostic`, `AnalysisResult`, etc.).
- **Pattern matching** (`is`, `switch` expressions) over explicit type checks and casts.
- **Collection expressions** (`[a, b, c]`) over `new List<>()` or array initializers where the target type is clear.
- **`IReadOnlyList<T>`** or **`IReadOnlyCollection<T>`** for all public collection properties — never `List<T>`.
- **`init`-only setters** on properties where post-construction mutation must be prevented.
- **One type per file** — each class, record, enum, or interface must be declared in its own file, with the file name matching the type name. Do not group multiple types in a single implementation file.

This rule applies even for small helper DTOs, response wrappers, and private/internal records.

## Readability and whitespace

- Keep one logical statement per line. Do not write multiple statements on one line,
  including compact `try`/`catch`, `if`, or loop bodies.
- Leave a blank line between logical statement groups such as setup, validation,
  transformation, external calls, and the final return.
- Expand long expression-bodied members into block-bodied members when the expression
  contains multiple operations, a conditional, a query chain, or a method call that wraps
  across lines.
- Break long method calls, object construction, conditionals, and collection expressions
  across meaningful boundaries. Keep source lines at or below 120 characters.
- Use the repository `.editorconfig` formatting settings and run `dotnet format` on changed
  C# files before requesting review.

---

## Naming conventions

| Element | Convention | Example |
| --- | --- | --- |
| Types, methods, properties | PascalCase | `GuidelineDefinition`, `AnalyzeAsync` |
| Private fields | `_camelCase` | `_analysisEngine` |
| Parameters, local variables | camelCase | `guidelineId`, `pipelineDocument` |
| Async methods | `…Async` suffix | `AnalyzeAsync`, `GetByIdAsync` |
| Interfaces | `I` prefix + noun/verb noun | `IGuidelineRule`, `IGuidelineRepository` |
| Rule classes | `{DescriptiveName}Rule` | `AbsoluteTemplatePathRule` |
| Test methods | `Method_GivenContext_ShouldOutcome` | `Analyze_GivenEmptyDocument_ShouldReturnNoDiagnostics` |

## XML documentation

Every `public` or `protected` member must have a complete XML doc comment:

```csharp
/// <summary>Analyses the given pipeline document and returns all diagnostics.</summary>
/// <param name="document">The parsed pipeline document to analyse.</param>
/// <returns>A read-only list of diagnostics; empty if no violations are found.</returns>
public IReadOnlyList<Diagnostic> Analyze(PipelineDocument document) { … }
```

## What to avoid

- `var` when the type is not immediately obvious from the right-hand side.
- `catch (Exception)` without re-throwing or a documented justification comment.
- `public` on anything that does not need to be public — prefer `internal`.
- `null!` suppressions — handle nullability explicitly using nullable annotations.
- `#pragma warning disable` without an inline comment explaining the permanent exception.
- Bare `set` accessor on a public property — use `init` or a computed property.
- Nested classes beyond simple `private` helper records.
- Static helper classes with many unrelated utility methods — prefer focused extension method classes.

## Agent behaviour (code-style scope)

Two rules from [`agent-behaviour.instructions.md`](agent-behaviour.instructions.md) are especially relevant when making code changes:

- **Dependency hygiene** — never add or upgrade a NuGet package silently. Flag the package
  name, version, license, and reason to the human before adding it. All versions must be
  declared in `Directory.Packages.props`; no per-project overrides without justification.
- **No silent warning suppressions** — `#pragma warning disable` and `null!` suppressions
  are code-style violations *and* a safety concern: they hide real issues. Add an explanatory
  comment and get human approval before suppressing any analyser diagnostic.
