# AGENTS.md — AzurePipelines.Guidelines.Core.Tests

## Purpose

Unit tests for `AzurePipelines.Guidelines.Core` — the domain layer.

## What gets tested here

- **Value object validation**: `GuidelineId` pattern matching, constructor exceptions.
- **Enum mappings**: severity → diagnostic level transformations.
- **Record equality**: domain models (`GuidelineDefinition`, `Diagnostic`, etc.) compare by value.
- **Collection immutability**: public `IReadOnlyList<T>` properties cannot be mutated after construction.

## Test naming

`MethodOrType_GivenContext_ShouldExpectedOutcome`

Examples:

```csharp
GuidelineId_GivenValidPattern_ShouldConstruct
GuidelineId_GivenInvalidPattern_ShouldThrowArgumentException
GuidelineSeverity_GivenDoOrDoNot_ShouldMapToError
```

## Coverage expectations

- **Every** `GuidelineId` validation branch (valid formats, invalid formats, null/empty).
- **Every** enum value has a documented mapping test.
- **All** record types test value equality (`Equals`, `GetHashCode`).
