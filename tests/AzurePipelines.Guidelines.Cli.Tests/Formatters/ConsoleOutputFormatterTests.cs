using AzurePipelines.Guidelines.Cli.Formatters;
using AzurePipelines.Guidelines.Core;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Cli.Tests.Formatters;

public sealed class ConsoleOutputFormatterTests
{
    private readonly ConsoleOutputFormatter _formatter = new();

    private static PipelineDocument CreateDocument(string filePath)
    {
        return new PipelineDocument(
            FilePath: filePath,
            RawContent: "trigger: none",
            Parameters: [],
            Variables: [],
            Stages: [],
            Jobs: [],
            Steps: []);
    }

    [Fact]
    public void FormatName_ReturnsConsole()
    {
        // Act
        string name = _formatter.FormatName;

        // Assert
        name.Should().Be("console");
    }

    [Fact]
    public void Format_WithEmptyResults_ReturnsNoFilesMessage()
    {
        // Arrange
        AnalysisResult[] results = Array.Empty<AnalysisResult>();

        // Act
        string output = _formatter.Format(results);

        // Assert
        output.Should().Contain("No files analysed");
    }

    [Fact]
    public void Format_WithNullResults_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => _formatter.Format(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Format_WithSingleCleanFile_ShowsOnlyCleanSummary()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");

        AnalysisResult result = new(
            Document: doc,
            Diagnostics: Array.Empty<Diagnostic>());

        // Act
        string output = _formatter.Format([result]);

        // Assert
        output.Should().Contain("Summary:");
        output.Should().Contain("Files scanned: 1");
        output.Should().Contain("No violations found");
        output.Should().NotContain("pipeline.yml"); // Clean files not listed in detail
    }

    [Fact]
    public void Format_WithSingleError_ShowsFileAndDiagnostic()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");

        GuidelineId ruleId = new("ADOG-STEPS-001");
        Diagnostic diagnostic = new(
            GuidelineId: ruleId,
            Severity: DiagnosticSeverity.Error,
            Message: "Macro syntax $(foo) in steps",
            FilePath: "pipeline.yml",
            Line: 12,
            Column: 1);

        AnalysisResult result = new(
            Document: doc,
            Diagnostics: [diagnostic]);

        // Act
        string output = _formatter.Format([result], useColor: false);

        // Assert
        output.Should().Contain("pipeline.yml");
        output.Should().Contain("Error");
        output.Should().Contain("ADOG-STEPS-001");
        output.Should().Contain("Line 12");
        output.Should().Contain("Macro syntax $(foo) in steps");
        output.Should().Contain("1 error");
    }

    [Fact]
    public void Format_WithMultipleSeverities_ShowsCorrectCounts()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");

        GuidelineId rule1 = new("ADOG-STEPS-001");
        GuidelineId rule2 = new("ADOG-JOBS-006");
        GuidelineId rule3 = new("ADOG-GENERAL-001");

        Diagnostic error = new(rule1, DiagnosticSeverity.Error, "Error message", "pipeline.yml", 10, 1);
        Diagnostic warning = new(rule2, DiagnosticSeverity.Warning, "Warning message", "pipeline.yml", 20, 1);
        Diagnostic info = new(rule3, DiagnosticSeverity.Info, "Info message", "pipeline.yml", 30, 1);

        AnalysisResult result = new(doc, [error, warning, info]);

        // Act
        string output = _formatter.Format([result], useColor: false);

        // Assert
        output.Should().Contain("3 (1 error, 1 warning, 1 info)");
    }

    [Fact]
    public void Format_WithMultipleFiles_GroupsByFile()
    {
        // Arrange
        PipelineDocument doc1 = CreateDocument("pipeline1.yml");
        PipelineDocument doc2 = CreateDocument("pipeline2.yml");

        GuidelineId rule = new("ADOG-STEPS-001");
        Diagnostic diag1 = new(rule, DiagnosticSeverity.Error, "Error in file 1", "pipeline1.yml", 5, 1);
        Diagnostic diag2 = new(rule, DiagnosticSeverity.Warning, "Warning in file 2", "pipeline2.yml", 10, 1);

        AnalysisResult result1 = new(doc1, [diag1]);
        AnalysisResult result2 = new(doc2, [diag2]);

        // Act
        string output = _formatter.Format([result1, result2], useColor: false);

        // Assert
        output.Should().Contain("pipeline1.yml");
        output.Should().Contain("Error in file 1");
        output.Should().Contain("pipeline2.yml");
        output.Should().Contain("Warning in file 2");
        output.Should().Contain("Files scanned: 2");
        output.Should().Contain("Violations: 2");
    }

    [Fact]
    public void Format_WithNoColor_DoesNotIncludeAnsiCodes()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");
        GuidelineId rule = new("ADOG-STEPS-001");
        Diagnostic diag = new(rule, DiagnosticSeverity.Error, "Test error", "pipeline.yml", 1, 1);
        AnalysisResult result = new(doc, [diag]);

        // Act
        string output = _formatter.Format([result], useColor: false);

        // Assert
        output.Should().NotContain("\x1b["); // ANSI escape sequence
    }

    [Fact]
    public void Format_WithColor_IncludesAnsiCodes()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");
        GuidelineId rule = new("ADOG-STEPS-001");
        Diagnostic diag = new(rule, DiagnosticSeverity.Error, "Test error", "pipeline.yml", 1, 1);
        AnalysisResult result = new(doc, [diag]);

        // Act
        string output = _formatter.Format([result], useColor: true);

        // Assert
        output.Should().Contain("\x1b["); // ANSI escape sequence
    }

    [Fact]
    public void Format_WithDiagnosticWithoutLine_ShowsFilePathOnly()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");
        GuidelineId rule = new("ADOG-GENERAL-001");
        Diagnostic diag = new(rule, DiagnosticSeverity.Warning, "File-level warning", "pipeline.yml", Line: null, Column: null);
        AnalysisResult result = new(doc, [diag]);

        // Act
        string output = _formatter.Format([result], useColor: false);

        // Assert
        output.Should().Contain("File-level warning");
        output.Should().NotContain("Line "); // Should not show line number
    }

    [Fact]
    public void Format_WithLargeResultSet_PerformsReasonably()
    {
        // Arrange - 100 files with 5 diagnostics each = 500 total diagnostics
        List<AnalysisResult> results = [];
        GuidelineId rule = new("ADOG-STEPS-001");

        for (int i = 0; i < 100; i++)
        {
            PipelineDocument doc = CreateDocument($"pipeline{i}.yml");
            List<Diagnostic> diagnostics = [];

            for (int j = 0; j < 5; j++)
            {
                diagnostics.Add(new Diagnostic(rule, DiagnosticSeverity.Warning, $"Warning {j}", $"pipeline{i}.yml", j + 1, 1));
            }

            results.Add(new AnalysisResult(doc, diagnostics));
        }

        // Act
        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
        string output = _formatter.Format(results, useColor: false);
        sw.Stop();

        // Assert
        output.Should().Contain("Files scanned: 100");
        output.Should().Contain("Violations: 500");
        sw.ElapsedMilliseconds.Should().BeLessThan(1000); // Should complete in under 1 second
    }

    [Fact]
    public void Format_WithMixedCleanAndViolationFiles_ShowsCorrectCleanCount()
    {
        // Arrange
        PipelineDocument doc1 = CreateDocument("clean1.yml");
        PipelineDocument doc2 = CreateDocument("violation.yml");
        PipelineDocument doc3 = CreateDocument("clean2.yml");

        GuidelineId rule = new("ADOG-STEPS-001");
        Diagnostic diag = new(rule, DiagnosticSeverity.Error, "Error", "violation.yml", 1, 1);

        AnalysisResult clean1 = new(doc1, []);
        AnalysisResult withViolation = new(doc2, [diag]);
        AnalysisResult clean2 = new(doc3, []);

        // Act
        string output = _formatter.Format([clean1, withViolation, clean2], useColor: false);

        // Assert
        output.Should().Contain("Files scanned: 3");
        output.Should().Contain("Clean files: 2");
        output.Should().Contain("Violations: 1");
    }

    [Fact]
    public void Format_WithOnlyCleanFiles_ShowsNoViolationsInSummary()
    {
        // Arrange
        PipelineDocument doc1 = CreateDocument("clean1.yml");
        PipelineDocument doc2 = CreateDocument("clean2.yml");

        AnalysisResult clean1 = new(doc1, []);
        AnalysisResult clean2 = new(doc2, []);

        // Act
        string output = _formatter.Format([clean1, clean2], useColor: false);

        // Assert
        output.Should().Contain("Files scanned: 2");
        output.Should().Contain("No violations found");
        output.Should().NotContain("clean1.yml"); // Clean files not listed
        output.Should().NotContain("clean2.yml");
    }

    [Fact]
    public void FormatGuidelineList_GivenNoGuidelines_ShouldShowEmptyMessage()
    {
        ConsoleFormatter.FormatGuidelineList([]).Should().Contain("No guidelines found");
    }

    [Fact]
    public void FormatGuidelineList_GivenOneGuideline_ShouldUseSingularSummary()
    {
        GuidelineDefinition guideline = new(
            new GuidelineId("ADOG-STEPS-001"), GuidelineCategory.Steps, GuidelineSeverity.Do,
            "Use templates", "Description", null, [], [], null, []);

        string output = ConsoleFormatter.FormatGuidelineList([guideline]);

        output.Should().Contain("ADOG-STEPS-001");
        output.Should().Contain("1 guideline.");
    }

    [Fact]
    public void FormatGuidelineDetail_GivenCompleteGuideline_ShouldIncludeOptionalSections()
    {
        GuidelineDefinition guideline = new(
            new GuidelineId("ADOG-STEPS-001"), GuidelineCategory.Steps, GuidelineSeverity.DoNot,
            "Use templates", "Description", "Rationale", [], [],
            new FixGuidance("Fix it", null, null), ["https://example.test"]);

        string output = ConsoleFormatter.FormatGuidelineDetail(guideline);

        output.Should().Contain("Rationale:");
        output.Should().Contain("Fix:");
        output.Should().Contain("References:");
        output.Should().Contain("do-not");
    }
}
