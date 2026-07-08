using AzurePipelines.Guidelines.Cli.Formatters;
using AzurePipelines.Guidelines.Core;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Cli.Tests.Formatters;

public sealed class CompactFormatterTests
{
    private readonly CompactFormatter _formatter = new();

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
    public void FormatName_ReturnsCompact()
    {
        // Act
        string name = _formatter.FormatName;

        // Assert
        name.Should().Be("compact");
    }

    [Fact]
    public void Format_WithEmptyResults_ReturnsNoFilesMessage()
    {
        // Arrange
        AnalysisResult[] results = [];

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
    public void Format_WithCleanFile_ReturnsNoViolations()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");
        AnalysisResult result = new(doc, []);

        // Act
        string output = _formatter.Format([result]);

        // Assert
        output.Should().Contain("No violations found");
    }

    [Fact]
    public void Format_WithSingleDiagnostic_ShowsCompactFormat()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");
        GuidelineId rule = new("ADOG-STEPS-001");
        Diagnostic diag = new(rule, DiagnosticSeverity.Error, "Macro syntax $(foo) in steps", "pipeline.yml", 12, 5);
        AnalysisResult result = new(doc, [diag]);

        // Act
        string output = _formatter.Format([result], useColor: false);

        // Assert
        output.Should().Be("pipeline.yml:12:5: error: [ADOG-STEPS-001] Macro syntax $(foo) in steps" + Environment.NewLine);
    }

    [Fact]
    public void Format_WithMissingLineNumber_ShowsDoubleColon()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");
        GuidelineId rule = new("ADOG-GENERAL-001");
        Diagnostic diag = new(rule, DiagnosticSeverity.Warning, "File-level warning", "pipeline.yml", Line: null, Column: null);
        AnalysisResult result = new(doc, [diag]);

        // Act
        string output = _formatter.Format([result], useColor: false);

        // Assert
        output.Should().Be("pipeline.yml::: warning: [ADOG-GENERAL-001] File-level warning" + Environment.NewLine);
    }

    [Fact]
    public void Format_WithMultipleDiagnostics_ShowsOneDiagnosticPerLine()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");
        GuidelineId rule1 = new("ADOG-STEPS-001");
        GuidelineId rule2 = new("ADOG-JOBS-006");

        Diagnostic diag1 = new(rule1, DiagnosticSeverity.Error, "Error message", "pipeline.yml", 10, 1);
        Diagnostic diag2 = new(rule2, DiagnosticSeverity.Warning, "Warning message", "pipeline.yml", 20, 5);

        AnalysisResult result = new(doc, [diag1, diag2]);

        // Act
        string output = _formatter.Format([result], useColor: false);

        // Assert
        string[] lines = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCount(2);
        lines[0].Should().Be("pipeline.yml:10:1: error: [ADOG-STEPS-001] Error message");
        lines[1].Should().Be("pipeline.yml:20:5: warning: [ADOG-JOBS-006] Warning message");
    }

    [Fact]
    public void Format_WithMultipleFiles_GroupsNaturally()
    {
        // Arrange
        PipelineDocument doc1 = CreateDocument("pipeline1.yml");
        PipelineDocument doc2 = CreateDocument("pipeline2.yml");

        GuidelineId rule = new("ADOG-STEPS-001");
        Diagnostic diag1 = new(rule, DiagnosticSeverity.Error, "Error in file 1", "pipeline1.yml", 5, 1);
        Diagnostic diag2 = new(rule, DiagnosticSeverity.Warning, "Warning in file 2", "pipeline2.yml", 10, 2);

        AnalysisResult result1 = new(doc1, [diag1]);
        AnalysisResult result2 = new(doc2, [diag2]);

        // Act
        string output = _formatter.Format([result1, result2], useColor: false);

        // Assert
        output.Should().Contain("pipeline1.yml:5:1: error:");
        output.Should().Contain("pipeline2.yml:10:2: warning:");
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
        output.Should().NotContain("\x1b[");
    }

    [Fact]
    public void Format_WithColor_IncludesAnsiCodesForSeverity()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");
        GuidelineId rule = new("ADOG-STEPS-001");
        Diagnostic diag = new(rule, DiagnosticSeverity.Error, "Test error", "pipeline.yml", 1, 1);
        AnalysisResult result = new(doc, [diag]);

        // Act
        string output = _formatter.Format([result], useColor: true);

        // Assert
        output.Should().Contain("\x1b[31m"); // Red for error
        output.Should().Contain("\x1b[0m");  // Reset
    }

    [Fact]
    public void Format_WithDifferentSeverities_ShowsCorrectSeverityText()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");
        GuidelineId rule1 = new("ADOG-STEPS-001");
        GuidelineId rule2 = new("ADOG-JOBS-006");
        GuidelineId rule3 = new("ADOG-GENERAL-001");

        Diagnostic error = new(rule1, DiagnosticSeverity.Error, "Error", "pipeline.yml", 1, 1);
        Diagnostic warning = new(rule2, DiagnosticSeverity.Warning, "Warning", "pipeline.yml", 2, 1);
        Diagnostic info = new(rule3, DiagnosticSeverity.Info, "Info", "pipeline.yml", 3, 1);

        AnalysisResult result = new(doc, [error, warning, info]);

        // Act
        string output = _formatter.Format([result], useColor: false);

        // Assert
        output.Should().Contain(": error: ");
        output.Should().Contain(": warning: ");
        output.Should().Contain(": info: ");
    }

    [Fact]
    public void Format_WithLargeResultSet_ProducesExpectedLineCount()
    {
        // Arrange - 50 files with 10 diagnostics each = 500 total
        List<AnalysisResult> results = [];
        GuidelineId rule = new("ADOG-STEPS-001");

        for (int i = 0; i < 50; i++)
        {
            PipelineDocument doc = CreateDocument($"pipeline{i}.yml");
            List<Diagnostic> diagnostics = [];

            for (int j = 0; j < 10; j++)
            {
                diagnostics.Add(new Diagnostic(rule, DiagnosticSeverity.Warning, $"Warning {j}", $"pipeline{i}.yml", j + 1, 1));
            }

            results.Add(new AnalysisResult(doc, diagnostics));
        }

        // Act
        string output = _formatter.Format(results, useColor: false);

        // Assert
        string[] lines = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCount(500); // Exactly 500 diagnostic lines
    }

    [Fact]
    public void Format_CompatibleWithGrepParsing()
    {
        // Arrange - ensure format is grep/tool-parseable: file:line:col: severity: [id] message
        PipelineDocument doc = CreateDocument("azure-pipelines.yml");
        GuidelineId rule = new("ADOG-STEPS-006");
        Diagnostic diag = new(rule, DiagnosticSeverity.Warning, "Step missing timeout", "azure-pipelines.yml", 42, 7);
        AnalysisResult result = new(doc, [diag]);

        // Act
        string output = _formatter.Format([result], useColor: false);

        // Assert
        output.Should().MatchRegex(@"^[\w\-\.\/\\]+:\d+:\d+: \w+: \[ADOG-[\w\-]+\] .+$");
    }
}
