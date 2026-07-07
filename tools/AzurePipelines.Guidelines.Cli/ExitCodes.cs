namespace AzurePipelines.Guidelines.Cli;

/// <summary>Process exit codes for the <c>adog</c> CLI.</summary>
internal static class ExitCodes
{
    /// <summary>Analysis ran successfully and no violations were found.</summary>
    internal const int Clean = 0;

    /// <summary>Analysis ran successfully and one or more violations were found.</summary>
    internal const int Violations = 1;

    /// <summary>
    /// A fatal error occurred before or during analysis (file not found, invalid YAML, etc.).
    /// </summary>
    internal const int Error = 2;
}
