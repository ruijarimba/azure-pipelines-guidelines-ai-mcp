namespace AzurePipelines.Guidelines.Cli;

internal sealed record AnalyzeCommandOptions(
    string[] Paths,
    string Format,
    string[]? Severity,
    string[]? Category,
    string? Output,
    bool SoftFail,
    bool NoColor,
    bool Quiet,
    bool Verbose);
