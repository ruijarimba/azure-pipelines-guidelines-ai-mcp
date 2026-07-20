namespace AzurePipelines.Guidelines.Cli;

/// <summary>
/// Effective options used to execute an analyze command.
/// </summary>
internal sealed record AnalyzeCommandOptions(
    string[] Paths,
    string Format,
    string[]? Severity,
    string[]? Category,
    string? Output,
    bool SoftFail,
    bool NoColor,
    bool Quiet,
    bool Verbose,
    bool IncludeHeuristics);
