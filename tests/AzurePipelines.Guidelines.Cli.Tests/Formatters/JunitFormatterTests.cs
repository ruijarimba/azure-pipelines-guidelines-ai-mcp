using System.Xml;
using AzurePipelines.Guidelines.Cli.Formatters;
using AzurePipelines.Guidelines.Core;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Cli.Tests.Formatters;

public sealed class JunitFormatterTests
{
    private readonly JunitFormatter _formatter = new();

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
    public void FormatName_ReturnsJunit()
    {
        // Act
        string name = _formatter.FormatName;

        // Assert
        name.Should().Be("junit");
    }

    [Fact]
    public void Format_WithEmptyResults_ReturnsValidXml()
    {
        // Arrange
        AnalysisResult[] results = [];

        // Act
        string output = _formatter.Format(results);

        // Assert
        XmlDocument doc = new();
        Action act = () => doc.LoadXml(output);
        act.Should().NotThrow();

        XmlElement? testsuite = doc.SelectSingleNode("//testsuite") as XmlElement;
        testsuite.Should().NotBeNull();
        testsuite!.GetAttribute("tests").Should().Be("0");
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
    public void Format_WithCleanFile_CreatesPassingTestCase()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");
        AnalysisResult result = new(doc, []);

        // Act
        string output = _formatter.Format([result]);

        // Assert
        XmlDocument xmlDoc = new();
        xmlDoc.LoadXml(output);

        XmlElement? testsuite = xmlDoc.SelectSingleNode("//testsuite") as XmlElement;
        testsuite!.GetAttribute("tests").Should().Be("1");
        testsuite.GetAttribute("failures").Should().Be("0");
        testsuite.GetAttribute("errors").Should().Be("0");

        XmlElement? testcase = xmlDoc.SelectSingleNode("//testcase") as XmlElement;
        testcase.Should().NotBeNull();
        testcase!.GetAttribute("name").Should().Contain("pipeline.yml");
        testcase.GetAttribute("name").Should().Contain("No violations");
    }

    [Fact]
    public void Format_WithSingleError_CreatesErrorTestCase()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");
        GuidelineId rule = new("ADOG-STEPS-001");
        Diagnostic diag = new(rule, DiagnosticSeverity.Error, "Macro syntax $(foo) in steps", "pipeline.yml", 12, 5);
        AnalysisResult result = new(doc, [diag]);

        // Act
        string output = _formatter.Format([result]);

        // Assert
        XmlDocument xmlDoc = new();
        xmlDoc.LoadXml(output);

        XmlElement? testsuite = xmlDoc.SelectSingleNode("//testsuite") as XmlElement;
        testsuite!.GetAttribute("tests").Should().Be("1");
        testsuite.GetAttribute("errors").Should().Be("1");
        testsuite.GetAttribute("failures").Should().Be("0");

        XmlElement? error = xmlDoc.SelectSingleNode("//error") as XmlElement;
        error.Should().NotBeNull();
        error!.GetAttribute("message").Should().Be("Macro syntax $(foo) in steps");
        error.GetAttribute("type").Should().Be("ADOG-STEPS-001");
    }

    [Fact]
    public void Format_WithSingleWarning_CreatesFailureTestCase()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");
        GuidelineId rule = new("ADOG-JOBS-006");
        Diagnostic diag = new(rule, DiagnosticSeverity.Warning, "Job missing timeout", "pipeline.yml", 20, 1);
        AnalysisResult result = new(doc, [diag]);

        // Act
        string output = _formatter.Format([result]);

        // Assert
        XmlDocument xmlDoc = new();
        xmlDoc.LoadXml(output);

        XmlElement? testsuite = xmlDoc.SelectSingleNode("//testsuite") as XmlElement;
        testsuite!.GetAttribute("tests").Should().Be("1");
        testsuite.GetAttribute("failures").Should().Be("1");
        testsuite.GetAttribute("errors").Should().Be("0");

        XmlElement? failure = xmlDoc.SelectSingleNode("//failure") as XmlElement;
        failure.Should().NotBeNull();
        failure!.GetAttribute("message").Should().Be("Job missing timeout");
    }

    [Fact]
    public void Format_WithMultipleDiagnostics_CreatesMultipleTestCases()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");
        GuidelineId rule1 = new("ADOG-STEPS-001");
        GuidelineId rule2 = new("ADOG-JOBS-006");

        Diagnostic error = new(rule1, DiagnosticSeverity.Error, "Error message", "pipeline.yml", 10, 1);
        Diagnostic warning = new(rule2, DiagnosticSeverity.Warning, "Warning message", "pipeline.yml", 20, 5);

        AnalysisResult result = new(doc, [error, warning]);

        // Act
        string output = _formatter.Format([result]);

        // Assert
        XmlDocument xmlDoc = new();
        xmlDoc.LoadXml(output);

        XmlNodeList? testcases = xmlDoc.SelectNodes("//testcase");
        testcases.Should().NotBeNull();
        testcases!.Count.Should().Be(2);
    }

    [Fact]
    public void Format_TestCaseName_IncludesFileAndLineNumber()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("azure-pipelines.yml");
        GuidelineId rule = new("ADOG-STEPS-006");
        Diagnostic diag = new(rule, DiagnosticSeverity.Warning, "Step missing timeout", "azure-pipelines.yml", 42, 7);
        AnalysisResult result = new(doc, [diag]);

        // Act
        string output = _formatter.Format([result]);

        // Assert
        XmlDocument xmlDoc = new();
        xmlDoc.LoadXml(output);

        XmlElement? testcase = xmlDoc.SelectSingleNode("//testcase") as XmlElement;
        testcase.Should().NotBeNull();
        testcase!.GetAttribute("name").Should().Be("azure-pipelines.yml:42 - ADOG-STEPS-006");
    }

    [Fact]
    public void Format_WithMissingLineNumber_OmitsLineFromTestName()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");
        GuidelineId rule = new("ADOG-GENERAL-001");
        Diagnostic diag = new(rule, DiagnosticSeverity.Info, "File-level info", "pipeline.yml", Line: null, Column: null);
        AnalysisResult result = new(doc, [diag]);

        // Act
        string output = _formatter.Format([result]);

        // Assert
        XmlDocument xmlDoc = new();
        xmlDoc.LoadXml(output);

        XmlElement? testcase = xmlDoc.SelectSingleNode("//testcase") as XmlElement;
        testcase!.GetAttribute("name").Should().Be("pipeline.yml - ADOG-GENERAL-001");
        testcase.GetAttribute("name").Should().NotContain(":");
    }

    [Fact]
    public void Format_FailureElement_ContainsCDataWithDetails()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");
        GuidelineId rule = new("ADOG-STEPS-001");
        Diagnostic diag = new(rule, DiagnosticSeverity.Warning, "Test warning", "pipeline.yml", 15, 3);
        AnalysisResult result = new(doc, [diag]);

        // Act
        string output = _formatter.Format([result]);

        // Assert
        XmlDocument xmlDoc = new();
        xmlDoc.LoadXml(output);

        XmlElement? failure = xmlDoc.SelectSingleNode("//failure") as XmlElement;
        string? cdata = failure!.InnerText;

        cdata.Should().Contain("Rule: ADOG-STEPS-001");
        cdata.Should().Contain("Severity: Warning");
        cdata.Should().Contain("File: pipeline.yml");
        cdata.Should().Contain("Line: 15");
        cdata.Should().Contain("Column: 3");
        cdata.Should().Contain("Message: Test warning");
    }

    [Fact]
    public void Format_WithMultipleFiles_CreatesTestCasesForAll()
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
        XmlDocument xmlDoc = new();
        xmlDoc.LoadXml(output);

        XmlNodeList? testcases = xmlDoc.SelectNodes("//testcase");
        testcases!.Count.Should().Be(2);
    }

    [Fact]
    public void Format_OutputIsWellFormedXml()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");
        GuidelineId rule = new("ADOG-STEPS-001");
        Diagnostic diag = new(rule, DiagnosticSeverity.Error, "Test", "pipeline.yml", 1, 1);
        AnalysisResult result = new(doc, [diag]);

        // Act
        string output = _formatter.Format([result]);

        // Assert
        XmlDocument xmlDoc = new();
        Action act = () => xmlDoc.LoadXml(output);
        act.Should().NotThrow();

        // Verify structure
        xmlDoc.SelectSingleNode("//testsuites").Should().NotBeNull();
        xmlDoc.SelectSingleNode("//testsuite").Should().NotBeNull();
    }

    [Fact]
    public void Format_TestsuiteAttributes_AreCorrect()
    {
        // Arrange
        PipelineDocument doc = CreateDocument("pipeline.yml");
        GuidelineId rule1 = new("ADOG-STEPS-001");
        GuidelineId rule2 = new("ADOG-JOBS-006");

        Diagnostic error = new(rule1, DiagnosticSeverity.Error, "Error", "pipeline.yml", 1, 1);
        Diagnostic warning = new(rule2, DiagnosticSeverity.Warning, "Warning", "pipeline.yml", 2, 1);

        AnalysisResult result = new(doc, [error, warning]);

        // Act
        string output = _formatter.Format([result]);

        // Assert
        XmlDocument xmlDoc = new();
        xmlDoc.LoadXml(output);

        XmlElement? testsuite = xmlDoc.SelectSingleNode("//testsuite") as XmlElement;
        testsuite!.GetAttribute("name").Should().Be("Azure Pipelines Guidelines Analysis");
        testsuite.GetAttribute("tests").Should().Be("2");
        testsuite.GetAttribute("errors").Should().Be("1");
        testsuite.GetAttribute("failures").Should().Be("1");
        testsuite.GetAttribute("skipped").Should().Be("0");
    }

    [Fact]
    public void Format_IgnoresUseColorParameter()
    {
        // Arrange - XML formatters should ignore color flag
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
    public void Format_WithMixedCleanAndViolationFiles_CountsBothTypes()
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
        XmlDocument xmlDoc = new();
        xmlDoc.LoadXml(output);

        XmlElement? testsuite = xmlDoc.SelectSingleNode("//testsuite") as XmlElement;
        testsuite!.GetAttribute("tests").Should().Be("2"); // 1 passing + 1 error
        testsuite.GetAttribute("errors").Should().Be("1");
        testsuite.GetAttribute("failures").Should().Be("0");
    }
}
