namespace AzurePipelines.Guidelines.Mcp.Tools;

internal sealed record FixDto(
    string Summary,
    string? Before,
    string? After);
