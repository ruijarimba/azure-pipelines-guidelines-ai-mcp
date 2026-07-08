using System.Text.Json;
using AzurePipelines.Guidelines.Cli.Formatters;
using AzurePipelines.Guidelines.Core;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Cli.Tests.Formatters;

public sealed class SarifFormatterTests
{
    private readonly SarifFormatter _formatter = new();

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
    public void FormatName_ReturnsSarif()
    {
        // Act
        string name = _formatter.FormatName;

        // Assert
        name.Should().Be("sarif");
    }

    [Fact]
    public void Format_WithEmptyResults_ReturnsValidSarif()
    {
        // Arrange
        AnalysisResult[] results = [];

        // Act
        string output = _formatter.Format(results);

        // Assert
        JsonDocument doc = JsonDocument.Parse(output);
        doc.RootElement.GetProperty("version").GetString().Should().Be("2.1.0");
        doc.RootElement.GetProperty("runs").GetArrayLength().Should().Be(1);
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
    public void Format_IncludesSarifVersion()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");
        AnalysisResult result = new(doc, []);

        // Act
        string output = _formatter.Format([result]);

        // Assert
        JsonDocument jsonDoc = JsonDocument.Parse(output);
        jsonDoc.RootElement.GetProperty("version").GetString().Should().Be("2.1.0");
        jsonDoc.RootElement.GetProperty("$schema").GetString().Should().Contain("sarif-schema-2.1.0.json");
    }

    [Fact]
    public void Format_IncludesToolMetadata()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");
        AnalysisResult result = new(doc, []);

        // Act
        string output = _formatter.Format([result]);

        // Assert
        JsonDocument jsonDoc = JsonDocument.Parse(output);
        JsonElement tool = jsonDoc.RootElement
            .GetProperty("runs")[0]
            .GetProperty("tool")
            .GetProperty("driver");

        tool.GetProperty("name").GetString().Should().Be("azure-pipelines-guidelines");
        tool.GetProperty("informationUri").GetString().Should().Contain("github.com");
        tool.GetProperty("version").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Format_WithSingleDiagnostic_CreatesResult()
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
        JsonElement results = jsonDoc.RootElement
            .GetProperty("runs")[0]
            .GetProperty("results");

        results.GetArrayLength().Should().Be(1);

        JsonElement sarifResult = results[0];
        sarifResult.GetProperty("ruleId").GetString().Should().Be("ADOG-STEPS-001");
        sarifResult.GetProperty("level").GetString().Should().Be("error");
        sarifResult.GetProperty("message").GetProperty("text").GetString().Should().Be("Macro syntax $(foo) in steps");
    }

    [Fact]
    public void Format_IncludesLocationInformation()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("azure-pipelines.yml");
        GuidelineId rule = new("ADOG-STEPS-006");
        Diagnostic diag = new(rule, DiagnosticSeverity.Warning, "Step missing timeout", "azure-pipelines.yml", 42, 7);
        AnalysisResult result = new(doc, [diag]);

        // Act
        string output = _formatter.Format([result]);

        // Assert
        JsonDocument jsonDoc = JsonDocument.Parse(output);
        JsonElement location = jsonDoc.RootElement
            .GetProperty("runs")[0]
            .GetProperty("results")[0]
            .GetProperty("locations")[0]
            .GetProperty("physicalLocation");

        location.GetProperty("artifactLocation").GetProperty("uri").GetString().Should().Be("azure-pipelines.yml");
        location.GetProperty("region").GetProperty("startLine").GetInt32().Should().Be(42);
        location.GetProperty("region").GetProperty("startColumn").GetInt32().Should().Be(7);
    }

    [Fact]
    public void Format_WithMissingLineNumber_OmitsRegion()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");
        GuidelineId rule = new("ADOG-GENERAL-001");
        Diagnostic diag = new(rule, DiagnosticSeverity.Info, "File-level info", "pipeline.yml", Line: null, Column: null);
        AnalysisResult result = new(doc, [diag]);

        // Act
        string output = _formatter.Format([result]);

        // Assert
        JsonDocument jsonDoc = JsonDocument.Parse(output);
        JsonElement location = jsonDoc.RootElement
            .GetProperty("runs")[0]
            .GetProperty("results")[0]
            .GetProperty("locations")[0]
            .GetProperty("physicalLocation");

        location.TryGetProperty("region", out _).Should().BeFalse();
    }

    [Fact]
    public void Format_MapsSeverityToSarifLevel()
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
        JsonDocument jsonDoc = JsonDocument.Parse(output);
        JsonElement results = jsonDoc.RootElement
            .GetProperty("runs")[0]
            .GetProperty("results");

        results[0].GetProperty("level").GetString().Should().Be("error");
        results[1].GetProperty("level").GetString().Should().Be("warning");
        results[2].GetProperty("level").GetString().Should().Be("note"); // Info maps to "note" in SARIF
    }

    [Fact]
    public void Format_IncludesRulesInToolDriver()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");
        GuidelineId rule1 = new("ADOG-STEPS-001");
        GuidelineId rule2 = new("ADOG-JOBS-006");

        Diagnostic diag1 = new(rule1, DiagnosticSeverity.Error, "Error", "pipeline.yml", 1, 1);
        Diagnostic diag2 = new(rule2, DiagnosticSeverity.Warning, "Warning", "pipeline.yml", 2, 1);

        AnalysisResult result = new(doc, [diag1, diag2]);

        // Act
        string output = _formatter.Format([result]);

        // Assert
        JsonDocument jsonDoc = JsonDocument.Parse(output);
        JsonElement rules = jsonDoc.RootElement
            .GetProperty("runs")[0]
            .GetProperty("tool")
            .GetProperty("driver")
            .GetProperty("rules");

        rules.GetArrayLength().Should().Be(2);

        // Check that rules have IDs and help URIs
        JsonElement rule = rules[0];
        rule.GetProperty("id").GetString().Should().MatchRegex("^ADOG-");
        rule.GetProperty("helpUri").GetString().Should().Contain("github.com");
    }

    [Fact]
    public void Format_WithMultipleFiles_CreatesResultsForAll()
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
        JsonElement results = jsonDoc.RootElement
            .GetProperty("runs")[0]
            .GetProperty("results");

        results.GetArrayLength().Should().Be(2);
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

        // Assert
        Action act = () => JsonDocument.Parse(output);
        act.Should().NotThrow();
    }

    [Fact]
    public void Format_UsesCamelCasePropertyNames()
    {
        // Arrange - use a diagnostic so ruleId appears
        PipelineDocument doc = CreateDocument("pipeline.yml");
        GuidelineId rule = new("ADOG-STEPS-001");
        Diagnostic diag = new(rule, DiagnosticSeverity.Error, "Test", "pipeline.yml", 1, 1);
        AnalysisResult result = new(doc, [diag]);

        // Act
        string output = _formatter.Format([result]);

        // Assert
        output.Should().Contain("\"version\"");
        output.Should().Contain("\"runs\"");
        output.Should().Contain("\"ruleId\"");
        output.Should().NotContain("\"RuleId\""); // Not PascalCase
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

        // Assert
        outputWithColor.Should().Be(outputWithoutColor);
    }

    [Fact]
    public void Format_WithCleanFile_ProducesEmptyResults()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");
        AnalysisResult result = new(doc, []);

        // Act
        string output = _formatter.Format([result]);

        // Assert
        JsonDocument jsonDoc = JsonDocument.Parse(output);
        JsonElement results = jsonDoc.RootElement
            .GetProperty("runs")[0]
            .GetProperty("results");

        results.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public void Format_RuleMetadata_IncludesShortDescription()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");
        GuidelineId rule = new("ADOG-STEPS-001");
        Diagnostic diag = new(rule, DiagnosticSeverity.Error, "Test", "pipeline.yml", 1, 1);
        AnalysisResult result = new(doc, [diag]);

        // Act
        string output = _formatter.Format([result]);

        // Assert
        JsonDocument jsonDoc = JsonDocument.Parse(output);
        JsonElement ruleDescriptor = jsonDoc.RootElement
            .GetProperty("runs")[0]
            .GetProperty("tool")
            .GetProperty("driver")
            .GetProperty("rules")[0];

        ruleDescriptor.GetProperty("shortDescription").GetProperty("text").GetString().Should().Contain("ADOG-STEPS-001");
    }
}
