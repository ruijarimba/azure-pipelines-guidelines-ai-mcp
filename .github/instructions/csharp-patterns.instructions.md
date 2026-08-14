---
applyTo: "**/*.cs"
---

# C# implementation patterns

These patterns apply to all C# code in this repository. They complement the rules in [`maintainability.instructions.md`](maintainability.instructions.md) and [`architecture.instructions.md`](architecture.instructions.md) with concrete, codebase-specific guidance.

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

Use `FrozenSet<T>` for any `static readonly` set that is written once and read many times. `FrozenSet<T>` is optimised for read-heavy workloads and is available from .NET 8.

```csharp
// Bad — mutable HashSet used as a constant lookup table.
private static readonly HashSet<string> _types =
    new(StringComparer.OrdinalIgnoreCase) { "string", "boolean" };

// Good — immutable, read-optimised.
private static readonly FrozenSet<string> _types =
    new[] { "string", "boolean" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
```

Use the `System.Collections.Frozen` namespace. No `PackageReference` is needed — the type is part of the .NET 8+ BCL.

---

## 3. Logging in `src/` libraries

`src/` projects reference `Microsoft.Extensions.Logging.Abstractions` only (see [architecture rules](architecture.instructions.md)). The `[LoggerMessage]` source generator requires the full `Microsoft.Extensions.Logging` package and must **not** be used in `src/`.

Use `LoggerMessage.Define` static delegates instead, and wrap each one in a small private `static void` helper so call sites read cleanly.

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

### 3.1. Do not log sensitive or user-controlled values

Never pass the following to any log call:

- Secrets, tokens, passwords, API keys, or connection strings.
- Raw YAML content or any value read directly from a user-supplied pipeline file.
- File paths or environment variable values that may contain secrets.

```csharp
// Bad — logs raw YAML that may contain secrets or inject noise into structured logs.
LogEvaluating(_logger, rule.GuidelineId.Value, document.RawContent);

// Good — log only stable identifiers under your control.
LogEvaluating(_logger, rule.GuidelineId.Value, document.FilePath);
```

Treat any value sourced from external input (YAML, environment variables, config files) as untrusted. Log identifiers and counts; never log raw values.

---

## 4. Diagnostic messages

### 4.1. Content fidelity to companion guidelines repository

**Every rule diagnostic message must be grounded in the authoritative guideline text from the companion repository:**  
[`https://github.com/ruijarimba/azure-pipelines-guidelines`](https://github.com/ruijarimba/azure-pipelines-guidelines)

- The "why does it matter?" sentence must paraphrase or quote the guideline's **Reason** section.
- The "what should the developer do?" sentence must paraphrase or quote the guideline's **Recommended approach** section.
- Do **not** add security advice, implementation details, or recommendations that are not present in the guideline.
- When in doubt, copy the exact wording from the guideline and edit for brevity.

**Example mismatch (wrong):**

Guideline [`donot-use-azurekeyvault-task.md`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/donot-use-azurekeyvault-task.md) says:
- **Reason:** Converts Key Vault secrets into pipeline variables, tightly couples job steps
- **Recommended approach:** Use variable groups linked to Key Vault + variables template + explicit step parameters

Diagnostic message says:  
> "Use a managed identity and access Key Vault from application code instead."

This introduces managed-identity guidance that the guideline does not contain — it is **wrong**.

**Example (correct):**

> "AzureKeyVault task detected. This task converts Key Vault secrets into pipeline variables and tightly couples job steps. Use a variable group linked to Key Vault, referenced from a variables template, with explicit step parameters instead."

---

**Rationale:** The companion guidelines repository represents significant domain expertise and editorial effort. Rule diagnostics must remain faithful to that content to preserve accuracy and avoid introducing unsupported advice.

### 4.2. Message structure

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

Never commit an `if` block whose body is empty or contains only a comment describing future work.

```csharp
// Bad — empty branch with a comment deferring the implementation.
if (options.IncludedCategories is { Count: > 0 })
{
    // category filtering not yet implemented
}

// Good — either implement it or remove the branch entirely.
```

If the feature is not yet implemented, remove the branch and record it as a `// TODO #<issue>:` on the options property or in a separate issue.

---

## 7. Prefer `sealed` for concrete types

Mark every concrete class `sealed` unless inheritance is explicitly required by a framework or test infrastructure.

- `sealed` communicates intent and enables JIT devirtualisation.
- If a class is unsealed, add an XML comment explaining why.

---

## 8. `ConfigureAwait` in library code

All `await` expressions in `src/` library code must use `.ConfigureAwait(false)` to avoid capturing the synchronisation context.

```csharp
// Bad — captures context unnecessarily in a library.
await SomeOperationAsync();

// Good.
await SomeOperationAsync().ConfigureAwait(false);
```

This rule does not apply to test code under `tests/`.

---

## 9. Debuggability

Debuggability is a first-class quality concern, on par with testability. Every type that appears in a watch window, test failure message, or log output must represent itself clearly without requiring a developer to expand every nested field.

> **ADR reference:** ADR-014 in [`docs/decisions.md`](../../docs/decisions.md) records
> the decision, the external sources that ground these rules, and the consequences.

---

### Rule 1: Override `ToString()` on every domain type

**DO override `ToString()`** on every `record` and `class` in `src/` whose instances are inspected during debugging or appear in test output or log messages.

```csharp
// Bad — auto-generated record dump: every field including large nested collections.
// StageNode { Name = Build, DisplayName = null, Jobs = [...], Variables = [...], ... }

// Good — concise developer summary (kept under the 120-char line limit).
public override string ToString()
{
    var line = Line?.ToString(CultureInfo.InvariantCulture) ?? "?";
    return $"Stage '{Name ?? "(unnamed)"}' (line {line}, {Jobs.Count} jobs)";
}
```

Rules for the body:

- Return a concise summary — enough to identify the instance, not a data dump.
- Do **not** throw exceptions from `ToString()`.
- Do **not** return `null` from `ToString()`.
- Do **not** include properties that are too large to scan at a glance (e.g. raw YAML or
  script bodies) or that merely duplicate the type name.
- Use `CultureInfo.InvariantCulture` for all numeric values to satisfy CA1305 (which is
  a build error in this project — see ADR-006). Add `using System.Globalization;`.
- Prefer `Line?.ToString(CultureInfo.InvariantCulture) ?? "?"` for optional line numbers.
- Prefer `"(unnamed)"` as a fallback for optional string identifiers.

---

### Rule 2: Apply `[DebuggerDisplay]` to every type with a `ToString()` override

**DO add `[DebuggerDisplay("{ToString(),nq}")]`** to every type that has a `ToString()` override. This controls the primary display in the debugger locals, watch, and autos panels and in hover tooltips, using `ToString()` as the single source of truth.

```csharp
// Bad — debugger shows the verbose auto-generated record representation.
public sealed record StageNode(string? Name, ...) { }

// Good — debugger shows exactly what ToString() returns.
[DebuggerDisplay("{ToString(),nq}")]
public sealed record StageNode(string? Name, ...)
{
    public override string ToString() => ...;
}
```

For types that are not records and whose `ToString()` already returns a single property value, reference that property directly to avoid an unnecessary method call:

```csharp
// GuidelineId.ToString() returns Value — use the property directly.
[DebuggerDisplay("{Value,nq}")]
public sealed class GuidelineId : IEquatable<GuidelineId> { ... }
```

---

### Rule 3: Suppress large or redundant properties with `[DebuggerBrowsable]`

**DO apply `[property: DebuggerBrowsable(DebuggerBrowsableState.Never)]`** to positional record parameters whose generated properties would clutter the watch window.

```csharp
// Bad — RawContent expands to a multi-kilobyte YAML blob in the watch window.
public sealed record PipelineDocument(string FilePath, string RawContent, ...)

// Good — suppressed in the debugger; the property is still accessible in code.
public sealed record PipelineDocument(
    string FilePath,
    [property: DebuggerBrowsable(DebuggerBrowsableState.Never)]
    string RawContent,
    ...)
```

Apply to properties that are:

- Large raw strings (YAML content, script bodies, full file text).
- Pure projections of data already visible through other browsable child nodes.

---

### Rule 4: Test every `ToString()` override

Every `ToString()` override must have dedicated tests in the corresponding `*Tests.cs` file (see [`testing.instructions.md`](testing.instructions.md)). Tests must cover:

- The expected output for a well-populated instance.
- Every logical branch in the formatting expression (named vs. unnamed, line known vs. unknown).
- Every null/missing-field fallback (`"(unnamed)"`, `"?"` line placeholder, empty value count).
