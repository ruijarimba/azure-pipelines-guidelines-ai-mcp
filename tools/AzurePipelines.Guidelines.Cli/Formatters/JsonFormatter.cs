using System.Text.Json;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Cli.Formatters;

/// <summary>
/// Formats analysis results as structured JSON.
/// Output structure: { summary: {...}, results: [{file, diagnostics: [...]}] }
/// </summary>
internal sealed class JsonAnalysisFormatter : IOutputFormatter
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <inheritdoc/>
    public string FormatName => "json";

    /// <inheritdoc/>
    public string Format(IReadOnlyList<AnalysisResult> results, bool useColor = true)
    {
        ArgumentNullException.ThrowIfNull(results);

        // Build the complete output structure before serialization so the JSON contract is explicit.
        JsonOutput output = new()
        {
            Summary = BuildSummary(results),
            Results = results.Select(r => new FileResult
            {
                File = r.Document.FilePath,
                Diagnostics = r.Diagnostics.Select(d => new DiagnosticOutput
                {
                    RuleId = d.GuidelineId.Value,
                    Severity = d.Severity switch
                    {
                        DiagnosticSeverity.Error => "error",
                        DiagnosticSeverity.Warning => "warning",
                        DiagnosticSeverity.Info => "info",
                        _ => "info",
                    },
                    Message = d.Message,
                    Line = d.Line,
                    Column = d.Column,
                }).ToArray(),
            }).ToArray(),
        };

        return JsonSerializer.Serialize(output, _jsonOptions);
    }

    private static Summary BuildSummary(IReadOnlyList<AnalysisResult> results)
    {
        int totalDiagnostics = 0;
        int errorCount = 0;
        int warningCount = 0;
        int infoCount = 0;

        foreach (AnalysisResult result in results)
        {
            totalDiagnostics += result.Diagnostics.Count;

            foreach (Diagnostic diagnostic in result.Diagnostics)
            {
                switch (diagnostic.Severity)
                {
                    case DiagnosticSeverity.Error:
                        errorCount++;
                        break;
                    case DiagnosticSeverity.Warning:
                        warningCount++;
                        break;
                    case DiagnosticSeverity.Info:
                        infoCount++;
                        break;
                }
            }
        }

        int filesWithViolations = results.Count(r => r.Diagnostics.Count > 0);
        int cleanFiles = results.Count - filesWithViolations;

        return new Summary
        {
            FilesScanned = results.Count,
            TotalViolations = totalDiagnostics,
            FilesWithViolations = filesWithViolations,
            CleanFiles = cleanFiles,
            Errors = errorCount,
            Warnings = warningCount,
            Info = infoCount,
        };
    }

    /// <summary>Root JSON output containing summary and per-file results.</summary>
    private sealed class JsonOutput
    {
        public required Summary Summary { get; init; }
        public required FileResult[] Results { get; init; }
    }

    /// <summary>Aggregate counts for the analyzed files.</summary>
    private sealed class Summary
    {
        public int FilesScanned { get; set; }
        public int TotalViolations { get; set; }
        public int FilesWithViolations { get; set; }
        public int CleanFiles { get; set; }
        public int Errors { get; set; }
        public int Warnings { get; set; }
        public int Info { get; set; }
    }

    /// <summary>Diagnostics associated with one analyzed file.</summary>
    private sealed class FileResult
    {
        public required string File { get; init; }
        public required DiagnosticOutput[] Diagnostics { get; init; }
    }

    /// <summary>Serialized representation of one diagnostic.</summary>
    private sealed class DiagnosticOutput
    {
        public required string RuleId { get; init; }
        public required string Severity { get; init; }
        public required string Message { get; init; }
        public int? Line { get; init; }
        public int? Column { get; init; }
    }
}
