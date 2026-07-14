---
applyTo: "**/*.cs"
---

# Maintainability rules

These rules apply to all C# code in this repository — agent-generated and human-written alike.
The goal is a codebase that any contributor can read, understand, and modify without needing
AI assistance or prior context.

> **Before changing any rule in this file:** re-read the reference sources recorded in
> [`docs/decisions.md` — ADR-011](../../docs/decisions.md) and update that ADR if the
> rationale changes.

---

## 1. File size

Keep files small enough that a reviewer can hold the whole file in their head.

| File type | Soft limit | Hard limit |
| --- | --- | --- |
| Production (`.cs` under `src/`) | 200 lines | 300 lines |
| Test (`.cs` under `tests/`) | 300 lines | 500 lines |

When a file approaches its soft limit, split it by responsibility **before** adding more code.
Do not ask whether to split — split first, then continue.

*Inspiration: Google C# Style Guide (one top-level type per file), Oracle Java Code
Conventions (classes over 500 lines are a review trigger).*

---

## 2. Method and property size

A method that does not fit on one screen is hard to reason about.

- **Soft limit:** 20 lines per method body.
- **Hard limit:** 40 lines per method body.
- If a method approaches the soft limit, extract a private helper with a descriptive name.
- Properties must not contain logic beyond a single expression. Move logic to a method.
- Constructor bodies must only assign fields or call `this(…)` / `base(…)`.
  Any other logic belongs in a factory method or a dedicated initialisation method.

*Inspiration: Google C# Style Guide, Oracle Java Code Conventions.*

---

## 3. Parameter count

Methods and constructors with many parameters are hard to call correctly and hard to read.

- **Maximum 4 parameters.** Beyond 4, introduce a parameter record or options object.
- Do not use `bool` parameters to switch method behaviour — split into two methods instead.
- Do not use `out` parameters in public APIs — return a result record instead.

```csharp
// Bad — boolean flag controlling behaviour, and too many parameters.
public IReadOnlyList<Diagnostic> Analyze(
    PipelineDocument document, bool includeWarnings, bool includeInfo, string? filter, CancellationToken ct)

// Good — split by concern; options object carries the knobs.
public IReadOnlyList<Diagnostic> Analyze(PipelineDocument document, AnalysisOptions options, CancellationToken ct)
```

*Inspiration: Microsoft .NET Framework Design Guidelines.*

---

## 4. Documentation

Every named type and member must be documented concisely so that a reader opening an unfamiliar
file can understand its purpose without reconstructing the entire call graph.

- Add XML documentation to classes, records, interfaces, enums, delegates, constructors, methods,
  properties, fields, events, and other named members.
- Use XML documentation to describe contracts, inputs, outputs, exceptions, and important
  constraints. Keep it short and use plain language.
- Document private implementation details when their purpose, invariant, or non-obvious decision
  is not clear from the code. Do not write comments that merely restate the implementation.
- XML documentation warnings may enforce public API coverage, but they do not replace the rule for
  private members. Private documentation remains subject to review unless a dedicated analyser is
  added.

*See ADR-011 — Human readability as a first-class requirement.*

---

## 5. One responsibility per unit

Each file, class, and method must have exactly one reason to change.

- **One top-level type per file.** This applies to every class, record, interface, enum,
  delegate, and struct, regardless of accessibility or whether it is sealed. A tightly coupled
  private nested DTO or options type may remain nested when it is only used by its containing
  type and keeping it local improves readability.
- **One concern per class.** If you need the word "and" to describe what a class does,
  split it.
- **One concern per method.** A method either queries state or changes state — not both
  (Command–Query Separation).
- **No `#region` blocks.** Their presence signals that the class is doing too much.
  Split the class instead.

*Inspiration: Microsoft C# coding conventions (`#region` discouraged), Google C# Style Guide.*

---

## 6. Comment discipline

Comments must explain **why**, never **what**. The code itself says what it does.
A comment that restates the code in plain English is noise — remove it.

```csharp
// Bad — restates the code.
// Increment the counter.
_count++;

// Good — explains a non-obvious decision.
// Start from 1 because the manifest uses 1-based line numbers.
var lineNumber = startLine + 1;
```

**XML doc comments** document the contract of named types and members. For private implementation
details, use XML documentation when it communicates a contract or constraint; otherwise use a
concise regular comment when the purpose is not obvious.

Additional rules:

- Start inline comments with a capital letter and end with a period.
- Leave one space between `//` and the comment text.
- Do not commit commented-out code. Delete it; version control preserves history.

*Inspiration: Microsoft C# coding conventions.*

---

## 7. No dead code or speculative scaffolding

Only commit code that is reachable, tested, and used.

- No `// TODO` or `// FIXME` without a linked issue number. Format: `// TODO #123: description`.
- No `throw new NotImplementedException()` in committed production code.
- No unused parameters, unused `using` directives, or unused private members.
- No empty `catch` blocks. Either handle the exception or let it propagate.
- No placeholder `switch` arms (`_ => null` or `default => throw`) unless all real cases are
  already handled in the same commit.

*Inspiration: Microsoft C# coding conventions, Microsoft .NET Framework Design Guidelines.*

---

## 8. No "clever" code

Prefer the obvious implementation over a terse or sophisticated one.
The next reader may not have your current context.

- Avoid LINQ chains longer than 3 operations on a single expression. Assign intermediate
  results to named local variables.
- Avoid nested ternary expressions. Use an `if`/`else` or a `switch` expression instead.
- Avoid deeply nested pattern-matching (`is { Prop: { Inner: … } }` beyond two levels).
  Extract a local variable or a helper method.
- Avoid method chaining that spans more than 3 lines without line-break clarity.
- Maximum line length: **120 characters.** Break earlier if it aids readability.

```csharp
// Bad — chained LINQ that requires careful reading to trace.
var ids = guidelines.Where(g => g.Severity == GuidelineSeverity.Do)
    .SelectMany(g => g.DetectionHints)
    .Where(h => h.Kind == DetectionKind.Regex)
    .Select(h => h.Pattern)
    .Distinct()
    .OrderBy(p => p)
    .ToList();

// Good — intermediate variables name each stage.
var errorGuidelines = guidelines.Where(g => g.Severity == GuidelineSeverity.Do);
var regexHints = errorGuidelines.SelectMany(g => g.DetectionHints)
                                .Where(h => h.Kind == DetectionKind.Regex);
var distinctPatterns = regexHints.Select(h => h.Pattern).Distinct().OrderBy(p => p).ToList();
```

*Inspiration: Google C# Style Guide (column limit 100; adapted to 120 for modern monitors),
Microsoft C# coding conventions.*

---

## 9. Change scope

Each pull request or agent task should be reviewable in a single sitting.

- **Aim for diffs under 400 lines changed** (additions + deletions, excluding generated files).
- If a task requires more than 400 lines, split it into independent steps and get approval
  for the split before writing any code.
- Each commit should do exactly one thing and have a subject line that completes the sentence
  "If applied, this commit will…".
- Do not mix refactoring with feature work in the same commit.

*Inspiration: standard code-review best practices; Google Engineering Practices guide.*
