using System.Text.Json;
using AzurePipelines.Guidelines.Cli.Formatters;
using AzurePipelines.Guidelines.Core;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Cli.Tests.Formatters;

public sealed class JsonAnalysisFormatterTests
{
    private readonly JsonAnalysisFormatter _formatter = new();

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
    public void FormatName_ReturnsJson()
    {
        // Act
        string name = _formatter.FormatName;

        // Assert
        name.Should().Be("json");
    }

    [Fact]
    public void Format_WithEmptyResults_ReturnsValidJson()
    {
        // Arrange
        AnalysisResult[] results = [];

        // Act
        string output = _formatter.Format(results);

        // Assert
        JsonDocument doc = JsonDocument.Parse(output);
        doc.RootElement.GetProperty("summary").GetProperty("filesScanned").GetInt32().Should().Be(0);
        doc.RootElement.GetProperty("results").GetArrayLength().Should().Be(0);
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
    public void Format_WithCleanFile_ShowsZeroViolations()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");
        AnalysisResult result = new(doc, []);

        // Act
        string output = _formatter.Format([result]);

        // Assert
        JsonDocument jsonDoc = JsonDocument.Parse(output);
        JsonElement summary = jsonDoc.RootElement.GetProperty("summary");
        summary.GetProperty("filesScanned").GetInt32().Should().Be(1);
        summary.GetProperty("totalViolations").GetInt32().Should().Be(0);
        summary.GetProperty("cleanFiles").GetInt32().Should().Be(1);
        summary.GetProperty("filesWithViolations").GetInt32().Should().Be(0);
    }

    [Fact]
    public void Format_WithSingleDiagnostic_IncludesDiagnosticInResults()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");
        GuidelineId rule = new("ADOG-STEPS-001");
        Diagnostic diag = new(rule, DiagnosticSeverity.Error, "Macro syntax $(foo) in steps", "pipeline.yml", 12, 5);
        AnalysisResult result = new(doc, [diag]);

        // Act
        string output = _formatter.Format([result]);

        // Assert
        JsonDocument jsonDoc = JsonDocument.Parse(output);
        JsonElement results = jsonDoc.RootElement.GetProperty("results");
        results.GetArrayLength().Should().Be(1);

        JsonElement fileResult = results[0];
        fileResult.GetProperty("file").GetString().Should().Be("pipeline.yml");

        JsonElement diagnostics = fileResult.GetProperty("diagnostics");
        diagnostics.GetArrayLength().Should().Be(1);

        JsonElement diagnostic = diagnostics[0];
        diagnostic.GetProperty("ruleId").GetString().Should().Be("ADOG-STEPS-001");
        diagnostic.GetProperty("severity").GetString().Should().Be("error");
        diagnostic.GetProperty("message").GetString().Should().Be("Macro syntax $(foo) in steps");
        diagnostic.GetProperty("line").GetInt32().Should().Be(12);
        diagnostic.GetProperty("column").GetInt32().Should().Be(5);
    }

    [Fact]
    public void Format_WithMissingLineColumn_OmitsNullFields()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");
        GuidelineId rule = new("ADOG-GENERAL-001");
        Diagnostic diag = new(rule, DiagnosticSeverity.Warning, "File-level warning", "pipeline.yml", Line: null, Column: null);
        AnalysisResult result = new(doc, [diag]);

        // Act
        string output = _formatter.Format([result]);

        // Assert
        JsonDocument jsonDoc = JsonDocument.Parse(output);
        JsonElement diagnostic = jsonDoc.RootElement
            .GetProperty("results")[0]
            .GetProperty("diagnostics")[0];

        diagnostic.GetProperty("line").ValueKind.Should().Be(JsonValueKind.Null);
        diagnostic.GetProperty("column").ValueKind.Should().Be(JsonValueKind.Null);
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
        string output = _formatter.Format([result]);

        // Assert
        JsonDocument jsonDoc = JsonDocument.Parse(output);
        JsonElement summary = jsonDoc.RootElement.GetProperty("summary");
        summary.GetProperty("totalViolations").GetInt32().Should().Be(3);
        summary.GetProperty("errors").GetInt32().Should().Be(1);
        summary.GetProperty("warnings").GetInt32().Should().Be(1);
        summary.GetProperty("info").GetInt32().Should().Be(1);
    }

    [Fact]
    public void Format_WithMultipleFiles_GroupsByFile()
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
        JsonDocument jsonDoc = JsonDocument.Parse(output);
        JsonElement results = jsonDoc.RootElement.GetProperty("results");
        results.GetArrayLength().Should().Be(2);

        results[0].GetProperty("file").GetString().Should().Be("pipeline1.yml");
        results[1].GetProperty("file").GetString().Should().Be("pipeline2.yml");
    }

    [Fact]
    public void Format_OutputIsWellFormedJson()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");
        GuidelineId rule = new("ADOG-STEPS-001");
        Diagnostic diag = new(rule, DiagnosticSeverity.Error, "Test", "pipeline.yml", 1, 1);
        AnalysisResult result = new(doc, [diag]);

        // Act
        string output = _formatter.Format([result]);

        // Assert - should parse without exception
        Action act = () => JsonDocument.Parse(output);
        act.Should().NotThrow();
    }

    [Fact]
    public void Format_UsesCamelCasePropertyNames()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");
        AnalysisResult result = new(doc, []);

        // Act
        string output = _formatter.Format([result]);

        // Assert
        output.Should().Contain("\"summary\"");
        output.Should().Contain("\"filesScanned\"");
        output.Should().Contain("\"totalViolations\"");
        output.Should().NotContain("\"FilesScanned\""); // Not PascalCase
    }

    [Fact]
    public void Format_IsIndented()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");
        AnalysisResult result = new(doc, []);

        // Act
        string output = _formatter.Format([result]);

        // Assert
        output.Should().Contain("  "); // Contains indentation
        output.Split(Environment.NewLine).Should().HaveCountGreaterThan(5); // Multiple lines due to indentation
    }

    [Fact]
    public void Format_WithMixedCleanAndViolationFiles_ShowsCorrectCounts()
    {
        // Arrange
        PipelineDocument doc1 = CreateDocument("clean.yml");
        PipelineDocument doc2 = CreateDocument("violation.yml");

        GuidelineId rule = new("ADOG-STEPS-001");
        Diagnostic diag = new(rule, DiagnosticSeverity.Error, "Error", "violation.yml", 1, 1);

        AnalysisResult clean = new(doc1, []);
        AnalysisResult withViolation = new(doc2, [diag]);

        // Act
        string output = _formatter.Format([clean, withViolation]);

        // Assert
        JsonDocument jsonDoc = JsonDocument.Parse(output);
        JsonElement summary = jsonDoc.RootElement.GetProperty("summary");
        summary.GetProperty("filesScanned").GetInt32().Should().Be(2);
        summary.GetProperty("cleanFiles").GetInt32().Should().Be(1);
        summary.GetProperty("filesWithViolations").GetInt32().Should().Be(1);
    }

    [Fact]
    public void Format_IgnoresUseColorParameter()
    {
        // Arrange - JSON formatters should ignore color flag
        PipelineDocument doc = CreateDocument("pipeline.yml");
        GuidelineId rule = new("ADOG-STEPS-001");
        Diagnostic diag = new(rule, DiagnosticSeverity.Error, "Test", "pipeline.yml", 1, 1);
        AnalysisResult result = new(doc, [diag]);

        // Act
        string outputWithColor = _formatter.Format([result], useColor: true);
        string outputWithoutColor = _formatter.Format([result], useColor: false);

        // Assert - Both outputs should be identical
        outputWithColor.Should().Be(outputWithoutColor);
    }

    [Fact]
    public void Format_WithMultipleFilesAndDiagnostics_ShouldPreserveFileAndDiagnosticOrder()
    {
        PipelineDocument firstDocument = CreateDocument("first.yml");
        PipelineDocument secondDocument = CreateDocument("second.yml");
        Diagnostic firstDiagnostic = new(new GuidelineId("ADOG-STEPS-001"), DiagnosticSeverity.Info, "First", "first.yml", 1, 2);
        Diagnostic secondDiagnostic = new(new GuidelineId("ADOG-JOBS-001"), DiagnosticSeverity.Warning, "Second", "second.yml", 2, 3);

        string output = _formatter.Format([
            new AnalysisResult(firstDocument, [firstDiagnostic]),
            new AnalysisResult(secondDocument, [secondDiagnostic])]);
        JsonElement results = JsonDocument.Parse(output).RootElement.GetProperty("results");

        results[0].GetProperty("file").GetString().Should().Be("first.yml");
        results[1].GetProperty("diagnostics")[0].GetProperty("severity").GetString().Should().Be("warning");
    }

    [Fact]
    public void FormatGuidelineListAndDetail_ShouldIncludeOptionalValues()
    {
        GuidelineDefinition guideline = new(
            new GuidelineId("ADOG-STEPS-001"), GuidelineCategory.Steps, GuidelineSeverity.Do,
            "Title", "Description", "Rationale", ["tag"], [], new FixGuidance("Fix", "before", "after"), ["reference"]);

        JsonDocument list = JsonDocument.Parse(JsonFormatter.FormatGuidelineList([guideline]));
        JsonDocument detail = JsonDocument.Parse(JsonFormatter.FormatGuidelineDetail(guideline));

        list.RootElement[0].GetProperty("id").GetString().Should().Be("ADOG-STEPS-001");
        detail.RootElement.GetProperty("tags")[0].GetString().Should().Be("tag");
        detail.RootElement.GetProperty("fix").GetProperty("after").GetString().Should().Be("after");
        detail.RootElement.GetProperty("references")[0].GetString().Should().Be("reference");
    }
}
