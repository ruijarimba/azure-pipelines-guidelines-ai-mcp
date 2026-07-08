using System.Globalization;
using System.Text.Json;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Cli.Formatters;

/// <summary>
/// Formats analysis results as SARIF 2.1.0 JSON for integration with
/// GitHub Code Scanning, Azure DevOps, and other static analysis tools.
/// </summary>
internal sealed class SarifFormatter : IOutputFormatter
{
    private const string SarifVersion = "2.1.0";
    private const string ToolName = "azure-pipelines-guidelines";
    private const string ToolVersion = "1.0.0"; // TODO: derive from assembly version

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public string FormatName => "sarif";

    public string Format(IReadOnlyList<AnalysisResult> results, bool useColor = true)
    {
        ArgumentNullException.ThrowIfNull(results);

        // Build SARIF structure
        SarifLog sarifLog = new()
        {
            Version = SarifVersion,
            Schema = "https://raw.githubusercontent.com/oasis-tcs/sarif-spec/master/Schemata/sarif-schema-2.1.0.json",
            Runs = [BuildRun(results)],
        };

        return JsonSerializer.Serialize(sarifLog, _jsonOptions);
    }

    private static Run BuildRun(IReadOnlyList<AnalysisResult> results)
    {
        // Collect unique rules from all diagnostics
        Dictionary<string, GuidelineId> rulesDict = [];
        List<Result> sarifResults = [];

        foreach (AnalysisResult result in results)
        {
            foreach (Diagnostic diagnostic in result.Diagnostics)
            {
                string ruleId = diagnostic.GuidelineId.Value;
                rulesDict.TryAdd(ruleId, diagnostic.GuidelineId);

                sarifResults.Add(BuildResult(diagnostic));
            }
        }

        return new Run
        {
            Tool = new Tool
            {
                Driver = new Driver
                {
                    Name = ToolName,
                    InformationUri = "https://github.com/ruijarimba/azure-pipelines-guidelines",
                    Version = ToolVersion,
                    Rules = rulesDict.Values.Select(BuildRule).ToArray(),
                },
            },
            Results = sarifResults.ToArray(),
        };
    }

    private static ReportingDescriptor BuildRule(GuidelineId guidelineId)
    {
        return new ReportingDescriptor
        {
            Id = guidelineId.Value,
            ShortDescription = new Message
            {
                Text = $"Guideline {guidelineId.Value}",
            },
            HelpUri = $"https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/data/guidelines.json#{guidelineId.Value}",
        };
    }

    private static Result BuildResult(Diagnostic diagnostic)
    {
        Result result = new()
        {
            RuleId = diagnostic.GuidelineId.Value,
            Level = MapSeverityToLevel(diagnostic.Severity),
            Message = new Message
            {
                Text = diagnostic.Message,
            },
            Locations =
            [
                new Location
                {
                    PhysicalLocation = new PhysicalLocation
                    {
                        ArtifactLocation = new ArtifactLocation
                        {
                            Uri = diagnostic.FilePath,
                        },
                        Region = diagnostic.Line.HasValue
                            ? new Region
                            {
                                StartLine = diagnostic.Line.Value,
                                StartColumn = diagnostic.Column ?? 1,
                            }
                            : null,
                    },
                },
            ],
        };

        return result;
    }

    private static string MapSeverityToLevel(DiagnosticSeverity severity)
    {
        return severity switch
        {
            DiagnosticSeverity.Error => "error",
            DiagnosticSeverity.Warning => "warning",
            DiagnosticSeverity.Info => "note",
            _ => "note",
        };
    }

    // SARIF 2.1.0 schema classes
    private sealed class SarifLog
    {
        public string Version { get; set; } = null!;
        [System.Text.Json.Serialization.JsonPropertyName("$schema")]
        public string Schema { get; set; } = null!;
        public Run[] Runs { get; set; } = null!;
    }

    private sealed class Run
    {
        public Tool Tool { get; set; } = null!;
        public Result[] Results { get; set; } = null!;
    }

    private sealed class Tool
    {
        public Driver Driver { get; set; } = null!;
    }

    private sealed class Driver
    {
        public string Name { get; set; } = null!;
        public string? InformationUri { get; set; }
        public string? Version { get; set; }
        public ReportingDescriptor[]? Rules { get; set; }
    }

    private sealed class ReportingDescriptor
    {
        public string Id { get; set; } = null!;
        public Message? ShortDescription { get; set; }
        public string? HelpUri { get; set; }
    }

    private sealed class Result
    {
        public string RuleId { get; set; } = null!;
        public string Level { get; set; } = null!;
        public Message Message { get; set; } = null!;
        public Location[] Locations { get; set; } = null!;
    }

    private sealed class Location
    {
        public PhysicalLocation PhysicalLocation { get; set; } = null!;
    }

    private sealed class PhysicalLocation
    {
        public ArtifactLocation ArtifactLocation { get; set; } = null!;
        public Region? Region { get; set; }
    }

    private sealed class ArtifactLocation
    {
        public string Uri { get; set; } = null!;
    }

    private sealed class Region
    {
        public int StartLine { get; set; }
        public int? StartColumn { get; set; }
    }

    private sealed class Message
    {
        public string Text { get; set; } = null!;
    }
}
