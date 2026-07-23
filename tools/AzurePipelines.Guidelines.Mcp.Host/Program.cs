using AzurePipelines.Guidelines.Mcp.Host;

// The MCP host is intentionally a thin dispatcher.
// Real startup logic lives in McpHostStartup so that transport-specific behaviour
// (web host for SSE, generic host for stdio) can be kept separate and tested.
string transport = GetTransport(args);

if (string.Equals(transport, "sse", StringComparison.OrdinalIgnoreCase))
{
    await McpHostStartup.RunSseAsync(args).ConfigureAwait(false);
}
else
{
    // stdio is the default and the only supported mode for Docker / CI usage.
    // SSE is provided only for local debugging from an IDE.
    await McpHostStartup.RunStdioAsync(args).ConfigureAwait(false);
}

static string GetTransport(string[] args)
{
    // First check the explicit command line flag used by launch profiles and scripts.
    for (int index = 0; index < args.Length; index++)
    {
        string argument = args[index];
        if (!string.Equals(argument, "--transport", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        if (index + 1 < args.Length)
        {
            return args[index + 1];
        }

        break;
    }

    // Then fall back to an environment variable so container images or CI wrappers
    // can change the transport without editing launch profiles.
    string? environmentTransport = Environment.GetEnvironmentVariable("MCP_TRANSPORT");
    return string.IsNullOrWhiteSpace(environmentTransport) ? "stdio" : environmentTransport;
}
