using AzurePipelines.Guidelines.Cli.Formatters;
using AzurePipelines.Guidelines.Core;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Cli.Tests.Formatters;

public sealed class MarkdownFormatterTests
{
    private readonly MarkdownFormatter _formatter = new();

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
    public void FormatName_ReturnsMarkdown()
    {
        // Act
        string name = _formatter.FormatName;

        // Assert
        name.Should().Be("markdown");
    }

    [Fact]
    public void Format_WithEmptyResults_ReturnsHeaderAndMessage()
    {
        // Arrange
        AnalysisResult[] results = [];

        // Act
        string output = _formatter.Format(results);

        // Assert
        output.Should().Contain("# Azure Pipelines Guidelines Analysis");
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
    public void Format_WithCleanFile_ShowsNoViolations()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");
        AnalysisResult result = new(doc, []);

        // Act
        string output = _formatter.Format([result]);

        // Assert
        output.Should().Contain("## Summary");
        output.Should().Contain("Files scanned | 1");
        output.Should().Contain("Total violations | 0");
        output.Should().Contain("✅ No violations found!");
    }

    [Fact]
    public void Format_WithSingleDiagnostic_CreatesTable()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");
        GuidelineId rule = new("ADOG-STEPS-001");
        Diagnostic diag = new(rule, DiagnosticSeverity.Error, "Macro syntax $(foo) in steps", "pipeline.yml", 12, 5);
        AnalysisResult result = new(doc, [diag]);

        // Act
        string output = _formatter.Format([result]);

        // Assert
        output.Should().Contain("## Violations");
        output.Should().Contain("### 📄 `pipeline.yml`");
        output.Should().Contain("| Line | Severity | Rule | Message |");
        output.Should().Contain("| 12 | ❌ Error |");
        output.Should().Contain("[ADOG-STEPS-001]");
        output.Should().Contain("Macro syntax $(foo) in steps");
    }

    [Fact]
    public void Format_RuleLinks_PointToGuidelineDocumentation()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");
        GuidelineId rule = new("ADOG-STEPS-006");
        Diagnostic diag = new(rule, DiagnosticSeverity.Warning, "Step missing timeout", "pipeline.yml", 42, 1);
        AnalysisResult result = new(doc, [diag]);

        // Act
        string output = _formatter.Format([result]);

        // Assert
        output.Should().Contain("[ADOG-STEPS-006](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/data/guidelines.json#ADOG-STEPS-006)");
    }

    [Fact]
    public void Format_WithMissingLineNumber_ShowsDash()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");
        GuidelineId rule = new("ADOG-GENERAL-001");
        Diagnostic diag = new(rule, DiagnosticSeverity.Info, "File-level info", "pipeline.yml", Line: null, Column: null);
        AnalysisResult result = new(doc, [diag]);

        // Act
        string output = _formatter.Format([result]);

        // Assert
        output.Should().MatchRegex(@"\|\s*-\s*\|\s*ℹ️ Info\s*\|");
    }

    [Fact]
    public void Format_WithMultipleSeverities_ShowsIconsAndCounts()
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
        string output = _formatter.Format([result]);

        // Assert
        output.Should().Contain("❌ Errors | 1");
        output.Should().Contain("⚠️ Warnings | 1");
        output.Should().Contain("ℹ️ Info | 1");
        output.Should().Contain("❌ Error");
        output.Should().Contain("⚠️ Warning");
        output.Should().Contain("ℹ️ Info");
    }

    [Fact]
    public void Format_WithMultipleFiles_CreatesSeparateSections()
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
        string output = _formatter.Format([result1, result2]);

        // Assert
        output.Should().Contain("### 📄 `pipeline1.yml`");
        output.Should().Contain("### 📄 `pipeline2.yml`");
    }

    [Fact]
    public void Format_EscapesPipeCharactersInMessages()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");
        GuidelineId rule = new("ADOG-STEPS-001");
        Diagnostic diag = new(rule, DiagnosticSeverity.Error, "Message with | pipe character", "pipeline.yml", 1, 1);
        AnalysisResult result = new(doc, [diag]);

        // Act
        string output = _formatter.Format([result]);

        // Assert
        output.Should().Contain(@"Message with \| pipe character");
        output.Should().NotContain("Message with | pipe character"); // Should be escaped
    }

    [Fact]
    public void Format_OutputIsValidMarkdown()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");
        GuidelineId rule = new("ADOG-STEPS-001");
        Diagnostic diag = new(rule, DiagnosticSeverity.Error, "Test", "pipeline.yml", 1, 1);
        AnalysisResult result = new(doc, [diag]);

        // Act
        string output = _formatter.Format([result]);

        // Assert - Check for valid Markdown structure
        output.Should().StartWith("# ");
        output.Should().Contain("## ");
        output.Should().Contain("### ");
        output.Should().Contain("|"); // Table syntax
    }

    [Fact]
    public void Format_SummaryTable_IsWellFormatted()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");
        GuidelineId rule = new("ADOG-STEPS-001");
        Diagnostic diag = new(rule, DiagnosticSeverity.Error, "Test", "pipeline.yml", 1, 1);
        AnalysisResult result = new(doc, [diag]);

        // Act
        string output = _formatter.Format([result]);

        // Assert
        output.Should().Contain("| Metric | Count |");
        output.Should().Contain("|--------|-------|");
        output.Should().MatchRegex(@"\| Files scanned \| \d+ \|");
        output.Should().MatchRegex(@"\| Total violations \| \d+ \|");
    }

    [Fact]
    public void Format_ViolationsTable_IsWellFormatted()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");
        GuidelineId rule = new("ADOG-STEPS-001");
        Diagnostic diag = new(rule, DiagnosticSeverity.Error, "Test", "pipeline.yml", 1, 1);
        AnalysisResult result = new(doc, [diag]);

        // Act
        string output = _formatter.Format([result]);

        // Assert
        output.Should().Contain("| Line | Severity | Rule | Message |");
        output.Should().Contain("|------|----------|------|---------|");
    }

    [Fact]
    public void Format_IgnoresUseColorParameter()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");
        GuidelineId rule = new("ADOG-STEPS-001");
        Diagnostic diag = new(rule, DiagnosticSeverity.Error, "Test", "pipeline.yml", 1, 1);
        AnalysisResult result = new(doc, [diag]);

        // Act
        string outputWithColor = _formatter.Format([result], useColor: true);
        string outputWithoutColor = _formatter.Format([result], useColor: false);

        // Assert - Markdown doesn't support colors, so output should be identical
        outputWithColor.Should().Be(outputWithoutColor);
    }

    [Fact]
    public void Format_WithMixedCleanAndViolationFiles_ShowsCleanCount()
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
        string output = _formatter.Format([clean1, withViolation, clean2]);

        // Assert
        output.Should().Contain("Files scanned | 3");
        output.Should().Contain("Clean files | 2");
        output.Should().Contain("### 📄 `violation.yml`");
        output.Should().NotContain("### 📄 `clean1.yml`");
        output.Should().NotContain("### 📄 `clean2.yml`");
    }

    [Fact]
    public void Format_WithOnlyCleanFiles_ShowsSuccessMessage()
    {
        // Arrange
        PipelineDocument doc1 = CreateDocument("clean1.yml");
        PipelineDocument doc2 = CreateDocument("clean2.yml");

        AnalysisResult clean1 = new(doc1, []);
        AnalysisResult clean2 = new(doc2, []);

        // Act
        string output = _formatter.Format([clean1, clean2]);

        // Assert
        output.Should().Contain("## ✅ No violations found!");
        output.Should().Contain("All analysed files comply with the Azure Pipelines guidelines");
        output.Should().NotContain("## Violations");
    }

    [Fact]
    public void Format_WithLargeResultSet_ProducesValidMarkdown()
    {
        // Arrange - 20 files with 5 diagnostics each
        List<AnalysisResult> results = [];
        GuidelineId rule = new("ADOG-STEPS-001");

        for (int i = 0; i < 20; i++)
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
        string output = _formatter.Format(results);

        // Assert
        output.Should().Contain("Files scanned | 20");
        output.Should().Contain("Total violations | 100");
        output.Should().StartWith("# ");
    }
}
