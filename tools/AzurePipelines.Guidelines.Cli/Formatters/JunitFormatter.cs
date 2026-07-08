using System.Globalization;
using System.Text;
using System.Xml;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Cli.Formatters;

/// <summary>
/// Formats analysis results as JUnit XML test results.
/// Each diagnostic is represented as a test case failure, allowing CI/CD systems
/// to display violations in their test results UI.
/// </summary>
internal sealed class JunitFormatter : IOutputFormatter
{
    public string FormatName => "junit";

    public string Format(IReadOnlyList<AnalysisResult> results, bool useColor = true)
    {
        ArgumentNullException.ThrowIfNull(results);

        int totalTests = 0;
        int failures = 0;
        int errors = 0;

        // Count diagnostics by severity
        foreach (AnalysisResult result in results)
        {
            foreach (Diagnostic diagnostic in result.Diagnostics)
            {
                totalTests++;
                if (diagnostic.Severity == DiagnosticSeverity.Error)
                {
                    errors++;
                }
                else
                {
                    failures++;
                }
            }
        }

        // Files with no violations count as passing tests
        int cleanFiles = results.Count(r => r.Diagnostics.Count == 0);
        totalTests += cleanFiles;

        StringBuilder sb = new();
        XmlWriterSettings settings = new()
        {
            Indent = true,
            IndentChars = "  ",
            OmitXmlDeclaration = false,
            Encoding = Encoding.UTF8,
        };

        using (XmlWriter writer = XmlWriter.Create(sb, settings))
        {
            writer.WriteStartDocument();

            // <testsuites>
            writer.WriteStartElement("testsuites");

            // <testsuite> - one suite for the entire analysis run
            writer.WriteStartElement("testsuite");
            writer.WriteAttributeString("name", "Azure Pipelines Guidelines Analysis");
            writer.WriteAttributeString("tests", totalTests.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("failures", failures.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("errors", errors.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("skipped", "0");

            foreach (AnalysisResult result in results)
            {
                if (result.Diagnostics.Count == 0)
                {
                    // Clean file - write passing test case
                    WritePassingTestCase(writer, result.Document.FilePath);
                }
                else
                {
                    // Write test case for each diagnostic
                    foreach (Diagnostic diagnostic in result.Diagnostics)
                    {
                        WriteFailingTestCase(writer, diagnostic);
                    }
                }
            }

            writer.WriteEndElement(); // </testsuite>
            writer.WriteEndElement(); // </testsuites>
            writer.WriteEndDocument();
        }

        return sb.ToString();
    }

    private static void WritePassingTestCase(XmlWriter writer, string filePath)
    {
        writer.WriteStartElement("testcase");
        writer.WriteAttributeString("name", $"{filePath} - No violations");
        writer.WriteAttributeString("classname", "AzurePipelinesGuidelines");
        writer.WriteEndElement(); // </testcase>
    }

    private static void WriteFailingTestCase(XmlWriter writer, Diagnostic diagnostic)
    {
        string testName = diagnostic.Line.HasValue
            ? $"{diagnostic.FilePath}:{diagnostic.Line.Value} - {diagnostic.GuidelineId.Value}"
            : $"{diagnostic.FilePath} - {diagnostic.GuidelineId.Value}";

        writer.WriteStartElement("testcase");
        writer.WriteAttributeString("name", testName);
        writer.WriteAttributeString("classname", "AzurePipelinesGuidelines");

        // Write failure or error element based on severity
        string elementName = diagnostic.Severity == DiagnosticSeverity.Error ? "error" : "failure";
        writer.WriteStartElement(elementName);
        writer.WriteAttributeString("message", diagnostic.Message);
        writer.WriteAttributeString("type", diagnostic.GuidelineId.Value);

        // Write full diagnostic details in CDATA
        StringBuilder details = new();
        details.AppendLine(CultureInfo.InvariantCulture, $"Rule: {diagnostic.GuidelineId.Value}");
        details.AppendLine(CultureInfo.InvariantCulture, $"Severity: {diagnostic.Severity}");
        details.AppendLine(CultureInfo.InvariantCulture, $"File: {diagnostic.FilePath}");
        if (diagnostic.Line.HasValue)
        {
            details.AppendLine(CultureInfo.InvariantCulture, $"Line: {diagnostic.Line.Value}");
        }
        if (diagnostic.Column.HasValue)
        {
            details.AppendLine(CultureInfo.InvariantCulture, $"Column: {diagnostic.Column.Value}");
        }
        details.AppendLine(CultureInfo.InvariantCulture, $"Message: {diagnostic.Message}");

        writer.WriteCData(details.ToString());

        writer.WriteEndElement(); // </failure> or </error>
        writer.WriteEndElement(); // </testcase>
    }
}
