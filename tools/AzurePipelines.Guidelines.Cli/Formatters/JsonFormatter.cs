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

    public string FormatName => "json";

    public string Format(IReadOnlyList<AnalysisResult> results, bool useColor = true)
    {
        ArgumentNullException.ThrowIfNull(results);

        // Build output structure
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

    // JSON output structure classes
    private sealed class JsonOutput
    {
        public Summary Summary { get; set; } = null!;
        public FileResult[] Results { get; set; } = null!;
    }

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

    private sealed class FileResult
    {
        public string File { get; set; } = null!;
        public DiagnosticOutput[] Diagnostics { get; set; } = null!;
    }

    private sealed class DiagnosticOutput
    {
        public string RuleId { get; set; } = null!;
        public string Severity { get; set; } = null!;
        public string Message { get; set; } = null!;
        public int? Line { get; set; }
        public int? Column { get; set; }
    }
}
