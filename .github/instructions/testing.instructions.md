---
applyTo: "tests/**/*.cs"
---

# Testing conventions

## Frameworks

| Purpose | Library |
| --- | --- |
| Test runner | xUnit |
| Assertions | FluentAssertions |
| Mocking / substitution | NSubstitute |

Never use other assertion or mocking libraries in this repository.
Never use xUnit's built-in `Assert` class; always use FluentAssertions.

## Test method naming

```
MethodName_GivenContext_ShouldExpectedOutcome
```

Examples:

```csharp
[Fact]
public void Analyze_GivenCompliantDocument_ShouldReturnNoDiagnostics() { … }

[Theory]
[InlineData(null)]
[InlineData("")]
public void Parse_GivenNullOrEmptyInput_ShouldThrowArgumentException(string? input) { … }

[Fact]
public void GetById_GivenUnknownRuleId_ShouldReturnNull() { … }
```

## Test structure (Arrange / Act / Assert)

Separate the three sections with a blank line and use `// Arrange`, `// Act`, `// Assert` comments:

```csharp
[Fact]
public void GetById_GivenKnownId_ShouldReturnGuideline()
{
    // Arrange
    var id = new GuidelineId("ADOG-STEPS-001");

    // Act
    var result = _sut.GetById(id);

    // Assert
    result.Should().NotBeNull();
    result!.Id.Should().Be(id);
}
```

## Coverage expectations

- Repository-wide line coverage must stay strictly above 95%. Every change must preserve or improve the current coverage baseline; code added without test coverage is not acceptable.
- Every logical branch (if/else, switch arm, ternary, null-coalescing) must have at least one test.
- Every behavior change must be tested for more than the happy path. At minimum, include tests for:
  - the normal success path
  - failure and invalid-input paths
  - edge cases and boundary conditions
- Edge cases: `null` inputs, empty collections, minimum/maximum values, duplicate entries.
- Error paths: exceptions thrown for invalid inputs, missing data, and boundary violations.
- No test may pass trivially — assert on a specific expected value, not just that a value exists.

### Measuring coverage

Run the following command from the repository root to collect coverage and generate a report:

```powershell
dotnet test AzurePipelinesGuidelines.slnx `
  --collect:"XPlat Code Coverage" `
  --results-directory ./coverage

# Generate an HTML report (requires reportgenerator global tool).
# Install once: dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator `
  -reports:"./coverage/**/coverage.cobertura.xml" `
  -targetdir:"./coverage/report" `
  -reporttypes:Html
```

The generated `./coverage/report/index.html` shows line, branch, and method coverage per file.
Before committing, confirm the repository-wide line coverage is strictly above 95%.

## What to avoid

- Shared mutable state between tests; each test must be fully independent and order-agnostic.
- `Thread.Sleep`, real timers, or real clocks — use fakes or time abstractions.
- Business logic in test helper methods — helpers may only build or arrange test data.
- Asserting on implementation details — test observable behaviour and public contracts only.
- Over-mocking — prefer real in-memory implementations over substitutes for value types and simple classes.

## Maintainability

Test files must remain as readable as production code.

- **One test class per production class.** Name it `{ProductionClassName}Tests` and place
  it in the same relative path under `tests/` as the production file is under `src/`.
- **One `[Fact]` or `[Theory]` per behaviour.** Do not test multiple independent behaviours
  in a single test method.
- **File size:** soft limit 300 lines, hard limit 500 lines per test file. When a file
  approaches the soft limit, split by scenario group into partial files or separate classes.
- **No catch-all test classes.** Do not create a single `UtilityTests` or `MiscTests` class.
  Every test class must correspond to one production type.
- **Test data builders or object mothers** are allowed for complex setup, but keep them in a
  dedicated `TestData/` folder inside the test project — not inline in test methods.
