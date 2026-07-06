---
applyTo: "**/*.cs"
---

# C# implementation patterns

These patterns apply to all C# code in this repository. They complement the rules in
[`maintainability.instructions.md`](maintainability.instructions.md) and
[`architecture.instructions.md`](architecture.instructions.md) with concrete,
codebase-specific guidance.

> **Before changing any rule in this file:** re-read the reference sources recorded in
> [`docs/decisions.md` — ADR-012](../../docs/decisions.md) and update that ADR if the
> rationale changes.

---

## 1. Implementing `IGuidelineRule`

Every rule class must follow this structure exactly.

```csharp
internal sealed class MyRule : IGuidelineRule
{
    private static readonly GuidelineId _id = new("ADOG-CATEGORY-NNN");

    /// <inheritdoc/>
    public GuidelineId GuidelineId => _id;

    /// <inheritdoc/>
    public async IAsyncEnumerable<Diagnostic> EvaluateAsync(
        PipelineDocument document,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        foreach (/* ... */)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return new Diagnostic(/* ... */);
        }
    }
}
```

**Rules:**

- Declare `GuidelineId` as a `private static readonly` field named `_id` and expose it
  via a property. Do not construct a new `GuidelineId` on every property access.
- The method signature must include `[EnumeratorCancellation]` on the token parameter.
- `ArgumentNullException.ThrowIfNull(document)` must be the first statement.
- Call `cancellationToken.ThrowIfCancellationRequested()` **inside** each loop iteration,
  immediately before the `yield return`.
- Do **not** add `await Task.CompletedTask;`. The `async` keyword on `IAsyncEnumerable`
  methods is implicit from the `yield` — the no-op `await` adds noise without value.
- If the method has no `yield return` reachable at compile time, remove `async` and return
  `AsyncEnumerable.Empty<Diagnostic>()`.
- Regex-based rules must be `partial` classes with `[GeneratedRegex]`. AST-based rules
  must not be `partial`.

---

## 2. Static lookup sets

Use `FrozenSet<T>` for any `static readonly` set that is written once and read many times.
`FrozenSet<T>` is optimised for read-heavy workloads and is available from .NET 8.

```csharp
// Bad — mutable HashSet used as a constant lookup table.
private static readonly HashSet<string> _types =
    new(StringComparer.OrdinalIgnoreCase) { "string", "boolean" };

// Good — immutable, read-optimised.
private static readonly FrozenSet<string> _types =
    new[] { "string", "boolean" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
```

Use the `System.Collections.Frozen` namespace. No `PackageReference` is needed — the type
is part of the .NET 8+ BCL.

---

## 3. Logging in `src/` libraries

`src/` projects reference `Microsoft.Extensions.Logging.Abstractions` only (see
[architecture rules](architecture.instructions.md)). The `[LoggerMessage]` source generator
requires the full `Microsoft.Extensions.Logging` package and must **not** be used in `src/`.

Use `LoggerMessage.Define` static delegates instead, and wrap each one in a small private
`static void` helper so call sites read cleanly.

```csharp
// Declaration — place near the top of the class, grouped together.
private static readonly Action<ILogger, string, string, Exception?> _logEvaluating =
    LoggerMessage.Define<string, string>(
        LogLevel.Debug,
        new EventId(1, nameof(LogEvaluating)),
        "Evaluating rule {RuleId} against {FilePath}");

// Private wrapper — keeps call sites clean.
private static void LogEvaluating(ILogger logger, string ruleId, string filePath)
    => _logEvaluating(logger, ruleId, filePath, null);

// Call site.
LogEvaluating(_logger, rule.GuidelineId.Value, document.FilePath);
```

Event IDs must be unique per class and stable (do not renumber existing IDs).

---

## 4. Diagnostic messages

Each diagnostic message must answer three questions in order:

1. **What was detected?** (one short phrase)
2. **Why does it matter?** (one sentence)
3. **What should the developer do instead?** (one sentence)

```csharp
// Bad — vague, repeats the line number already in the Line field.
$"Variable name at line {line} looks like a secret."

// Good — answers what / why / fix.
"Variable name looks like a secret (password, token, or key). " +
"Storing secrets as plain-text pipeline variables risks exposure in logs. " +
"Use a secret variable group or Azure Key Vault instead."
```

**Additional rules:**

- Never include `line`, `column`, or file path in the message text. Those values are already
  in the `Diagnostic` record's structured fields.
- Write in plain English. Prefer short sentences (≤ 25 words each).
- Do not end the message with a full stop if the last sentence already ends in a noun phrase.
  Keep the full stop if it is a complete sentence.

---

## 5. Regex patterns in rules

**Use two small patterns over one large alternation.**

```csharp
// Bad — one inscrutable alternation mixing two unrelated YAML styles.
[GeneratedRegex(
    @"(?:name:\s*\S*(?:password|secret)\S*[^\n]*\n\s*value:\s*\S+|(?:password|secret)\s*:\s*\S+)")]
private static partial Regex SecretPattern();

// Good — two focused patterns, each with its own comment.
// Matches the sequence block style:  name: apiKey\n    value: plaintext
[GeneratedRegex(@"name:\s*\S*(?:password|secret|token)\S*[^\n]*\n\s*value:\s*\S+",
    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
private static partial Regex BlockStyleSecretPattern();

// Matches the mapping style:  password: plaintext
[GeneratedRegex(@"(?:password|secret|token)\s*:\s*\S+",
    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
private static partial Regex MappingStyleSecretPattern();
```

**Mandatory comment format for every non-trivial regex:**

```csharp
// Matches:   <what it matches — one short phrase>
// Excludes:  <what it intentionally skips — e.g. "cross-repo refs (@alias)">
// Example:   <a literal YAML string the pattern should match>
```

**Flags:**

- Always include `RegexOptions.CultureInvariant`.
- Use `RegexOptions.IgnoreCase` in the options flags. Do **not** embed `(?i)` in the pattern.
- Use `RegexOptions.Multiline` when `^` / `$` or line-based matching is needed.
- Always use `[GeneratedRegex]` for patterns in `partial` rule classes — never `new Regex(...)`.

---

## 6. No empty conditional branches

Never commit an `if` block whose body is empty or contains only a comment describing
future work.

```csharp
// Bad — empty branch with a comment deferring the implementation.
if (options.IncludedCategories is { Count: > 0 })
{
    // category filtering not yet implemented
}

// Good — either implement it or remove the branch entirely.
```

If the feature is not yet implemented, remove the branch and record it as a
`// TODO #<issue>:` on the options property or in a separate issue.

---

## 7. Prefer `sealed` for concrete types

Mark every concrete class `sealed` unless inheritance is explicitly required by a
framework or test infrastructure.

- `sealed` communicates intent and enables JIT devirtualisation.
- If a class is unsealed, add an XML comment explaining why.

---

## 8. `ConfigureAwait` in library code

All `await` expressions in `src/` library code must use `.ConfigureAwait(false)` to avoid
capturing the synchronisation context.

```csharp
// Bad — captures context unnecessarily in a library.
await SomeOperationAsync();

// Good.
await SomeOperationAsync().ConfigureAwait(false);
```

This rule does not apply to test code under `tests/`.
