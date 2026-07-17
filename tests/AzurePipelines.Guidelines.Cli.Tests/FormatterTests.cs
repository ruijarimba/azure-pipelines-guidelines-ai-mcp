using System.Text.Json;
using AzurePipelines.Guidelines.Cli;
using AzurePipelines.Guidelines.Core;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Cli.Tests;

public sealed class FormatterTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static PipelineDocument EmptyDocument(string filePath = "pipeline.yml") =>
        new(
            Jobs: [],
            Stages: [],
            Steps: [],
            Variables: [],
            Parameters: [],
            RawContent: string.Empty,
            FilePath: filePath);

    private static Diagnostic MakeDiagnostic(
        string id = "ADOG-STEPS-006",
        DiagnosticSeverity severity = DiagnosticSeverity.Error,
        string message = "Missing timeout.",
        string filePath = "pipeline.yml",
        int? line = 3) =>
        new(new GuidelineId(id), severity, message, filePath, line, Column: null);

    // ── ConsoleFormatter ──────────────────────────────────────────────────────

    [Fact]
    public void ConsoleFormatter_GivenCleanResult_ShouldPrintNoViolationsMessage()
    {
        // Arrange
        AnalysisResult result = new(EmptyDocument(), []);

        // Act
        string output = ConsoleFormatter.Format(result);

        // Assert
        output.Should().Contain("No violations");
    }

    [Fact]
    public void ConsoleFormatter_GivenOneDiagnostic_ShouldContainRuleIdAndMessage()
    {
        // Arrange
        Diagnostic diag = MakeDiagnostic();
        AnalysisResult result = new(EmptyDocument(), [diag]);

        // Act
        string output = ConsoleFormatter.Format(result);

        // Assert
        output.Should().Contain("ADOG-STEPS-006");
        output.Should().Contain("Missing timeout.");
        output.Should().Contain("error:");
        output.Should().Contain("pipeline.yml:3");
    }

    [Fact]
    public void ConsoleFormatter_GivenNullLine_ShouldOmitLineNumber()
    {
        // Arrange
        Diagnostic diag = MakeDiagnostic(line: null);
        AnalysisResult result = new(EmptyDocument(), [diag]);

        // Act
        string output = ConsoleFormatter.Format(result);

        // Assert
        output.Should().Contain("pipeline.yml)");
        output.Should().NotContain("pipeline.yml:");
    }

    [Fact]
    public void ConsoleFormatter_GivenMultipleDiagnostics_ShouldPrintViolationCount()
    {
        // Arrange
        AnalysisResult result = new(EmptyDocument(), [
            MakeDiagnostic("ADOG-STEPS-006"),
            MakeDiagnostic("ADOG-JOBS-006"),
        ]);

        // Act
        string output = ConsoleFormatter.Format(result);

        // Assert
        output.Should().Contain("2 violations");
    }

    // ── JsonFormatter ─────────────────────────────────────────────────────────

    [Fact]
    public void JsonFormatter_GivenCleanResult_ShouldReturnEmptyArray()
    {
        // Arrange
        AnalysisResult result = new(EmptyDocument(), []);

        // Act
        string output = JsonFormatter.Format(result);

        // Assert
        JsonElement[] items = JsonSerializer.Deserialize<JsonElement[]>(output)!;
        items.Should().BeEmpty();
    }

    [Fact]
    public void JsonFormatter_GivenOneDiagnostic_ShouldContainExpectedFields()
    {
        // Arrange
        Diagnostic diag = MakeDiagnostic(severity: DiagnosticSeverity.Warning, line: 7);
        AnalysisResult result = new(EmptyDocument(), [diag]);

        // Act
        string output = JsonFormatter.Format(result);

        // Assert
        JsonElement[] items = JsonSerializer.Deserialize<JsonElement[]>(output)!;
        items.Should().HaveCount(1);

        JsonElement item = items[0];
        item.GetProperty("ruleId").GetString().Should().Be("ADOG-STEPS-006");
        item.GetProperty("severity").GetString().Should().Be("warning");
        item.GetProperty("message").GetString().Should().Be("Missing timeout.");
        item.GetProperty("line").GetInt32().Should().Be(7);
    }

    [Fact]
    public void JsonFormatter_GivenNullLine_ShouldOmitLineField()
    {
        // Arrange
        Diagnostic diag = MakeDiagnostic(line: null);
        AnalysisResult result = new(EmptyDocument(), [diag]);

        // Act
        string output = JsonFormatter.Format(result);

        // Assert
        JsonElement[] items = JsonSerializer.Deserialize<JsonElement[]>(output)!;
        items[0].TryGetProperty("line", out _).Should().BeFalse();
    }

    [Fact]
    public void ConsoleFormatter_GivenGuidelineList_ShouldUsePluralAndSeverityLabels()
    {
        GuidelineDefinition[] guidelines =
        [
            new(new GuidelineId("ADOG-GENERAL-001"), GuidelineCategory.General, GuidelineSeverity.DoNot, "Do not", "Description", null, [], [], null, []),
            new(new GuidelineId("ADOG-STEPS-001"), GuidelineCategory.Steps, GuidelineSeverity.Avoid, "Avoid", "Description", null, [], [], null, []),
            new(new GuidelineId("ADOG-JOBS-001"), GuidelineCategory.Jobs, GuidelineSeverity.Consider, "Consider", "Description", null, [], [], null, []),
        ];

        string output = ConsoleFormatter.FormatGuidelineList(guidelines);

        output.Should().Contain("do-not");
        output.Should().Contain("avoid");
        output.Should().Contain("consider");
        output.Should().Contain("3 guidelines.");
    }

    [Fact]
    public void ConsoleFormatter_GivenGuidelineWithRationaleFixAndReferences_ShouldShowDetails()
    {
        GuidelineDefinition guideline = new(
            new GuidelineId("ADOG-STEPS-001"), GuidelineCategory.Steps, GuidelineSeverity.Consider,
            "Title", "Description", "Rationale", [], [], new FixGuidance("Fix", null, null), ["reference"]);

        string output = ConsoleFormatter.FormatGuidelineDetail(guideline);

        output.Should().Contain("Rationale:");
        output.Should().Contain("Fix:");
        output.Should().Contain("reference");
    }
}
