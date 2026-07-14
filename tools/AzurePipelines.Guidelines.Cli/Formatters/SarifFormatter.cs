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
    private const string _sarifVersion = "2.1.0";
    private const string _toolName = "azure-pipelines-guidelines";
    private const string _toolVersion = "1.0.0"; // TODO: derive from assembly version

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

        // Build the complete SARIF structure before serialization.
        SarifLog sarifLog = new()
        {
            Version = _sarifVersion,
            Schema = "https://raw.githubusercontent.com/oasis-tcs/sarif-spec/master/Schemata/sarif-schema-2.1.0.json",
            Runs = [BuildRun(results)],
        };

        return JsonSerializer.Serialize(sarifLog, _jsonOptions);
    }

    private static Run BuildRun(IReadOnlyList<AnalysisResult> results)
    {
        // Keep one SARIF rule descriptor per guideline while preserving every diagnostic result.
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
                    Name = _toolName,
                    InformationUri = "https://github.com/ruijarimba/azure-pipelines-guidelines",
                    Version = _toolVersion,
                    Rules = rulesDict.Values.Select(BuildRule).ToArray(),
                },
            },
            Results = sarifResults.ToArray(),
        };
    }

    /// <summary>Builds the SARIF descriptor for one guideline.</summary>
    /// <param name="guidelineId">The guideline identifier.</param>
    /// <returns>A SARIF rule descriptor.</returns>
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

    /// <summary>Builds one SARIF result from a diagnostic.</summary>
    /// <param name="diagnostic">The diagnostic to convert.</param>
    /// <returns>A SARIF result payload.</returns>
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

    /// <summary>Maps the domain diagnostic severity to a SARIF level.</summary>
    /// <param name="severity">The diagnostic severity.</param>
    /// <returns>The SARIF level name.</returns>
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

    // These nested classes model the private SARIF 2.1.0 serialization contract.

    /// <summary>Represents the root SARIF document.</summary>
    private sealed class SarifLog
    {
        public string Version { get; set; } = null!;
        [System.Text.Json.Serialization.JsonPropertyName("$schema")]
        public string Schema { get; set; } = null!;
        public Run[] Runs { get; set; } = null!;
    }

    /// <summary>Represents one SARIF analysis run.</summary>
    private sealed class Run
    {
        public Tool Tool { get; set; } = null!;
        public Result[] Results { get; set; } = null!;
    }

    /// <summary>Represents the SARIF tool wrapper.</summary>
    private sealed class Tool
    {
        public Driver Driver { get; set; } = null!;
    }

    /// <summary>Represents the SARIF tool driver.</summary>
    private sealed class Driver
    {
        public string Name { get; set; } = null!;
        public string? InformationUri { get; set; }
        public string? Version { get; set; }
        public ReportingDescriptor[]? Rules { get; set; }
    }

    /// <summary>Represents a SARIF rule descriptor.</summary>
    private sealed class ReportingDescriptor
    {
        public string Id { get; set; } = null!;
        public Message? ShortDescription { get; set; }
        public string? HelpUri { get; set; }
    }

    /// <summary>Represents one SARIF diagnostic result.</summary>
    private sealed class Result
    {
        public string RuleId { get; set; } = null!;
        public string Level { get; set; } = null!;
        public Message Message { get; set; } = null!;
        public Location[] Locations { get; set; } = null!;
    }

    /// <summary>Represents a SARIF result location.</summary>
    private sealed class Location
    {
        public PhysicalLocation PhysicalLocation { get; set; } = null!;
    }

    /// <summary>Represents the physical location of a SARIF result.</summary>
    private sealed class PhysicalLocation
    {
        public ArtifactLocation ArtifactLocation { get; set; } = null!;
        public Region? Region { get; set; }
    }

    /// <summary>Represents the file location of a SARIF result.</summary>
    private sealed class ArtifactLocation
    {
        public string Uri { get; set; } = null!;
    }

    /// <summary>Represents the source region of a SARIF result.</summary>
    private sealed class Region
    {
        public int StartLine { get; set; }
        public int? StartColumn { get; set; }
    }

    /// <summary>Represents a SARIF message.</summary>
    private sealed class Message
    {
        public string Text { get; set; } = null!;
    }
}
