using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace AzurePipelines.Guidelines.Mcp.Host;

/// <summary>
/// Logs a narrowly scoped, safe subset of outbound MCP protocol responses for local diagnostics.
/// </summary>
/// <remarks>
/// The server never logs requests. It records only the identifiers of allowlisted incoming discovery
/// requests and then logs the corresponding successful outbound responses. This avoids writing
/// inline YAML, file paths, tool arguments, resource content, prompt arguments, notifications, or
/// error payloads to the console. The filters do not alter or suppress protocol traffic.
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Instantiated through dependency injection when MCP_LOG_RESPONSES is enabled.")]
internal sealed partial class DiscoveryResponseLogger
{
    private const int _maximumTrackedRequests = 64;
    private const int _maximumPayloadLength = 32_768;

    private static readonly HashSet<string> _allowedMethods = new(StringComparer.Ordinal)
    {
        "initialize",
        "tools/list",
        "resources/list",
        "resources/templates/list",
        "prompts/list",
    };

    private readonly ConcurrentDictionary<string, string> _methodsByRequestId = new();
    private readonly ILogger<DiscoveryResponseLogger> _logger;

    public DiscoveryResponseLogger(ILogger<DiscoveryResponseLogger> logger)
    {
        _logger = logger;
    }

    internal static McpMessageHandler CreateIncomingFilter(McpMessageHandler next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return async (context, cancellationToken) =>
        {
            DiscoveryResponseLogger? logger = context.Services?.GetService<DiscoveryResponseLogger>();
            if (logger is not null && context.JsonRpcMessage is JsonRpcRequest request)
            {
                logger.RecordRequest(request);
            }

            await next(context, cancellationToken).ConfigureAwait(false);
        };
    }

    internal static McpMessageHandler CreateOutgoingFilter(McpMessageHandler next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return async (context, cancellationToken) =>
        {
            DiscoveryResponseLogger? logger = context.Services?.GetService<DiscoveryResponseLogger>();
            if (logger is not null && context.JsonRpcMessage is JsonRpcResponse response)
            {
                logger.LogResponse(response);
            }

            await next(context, cancellationToken).ConfigureAwait(false);
        };
    }

    internal void RecordRequest(JsonRpcRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!IsDiscoveryMethod(request.Method))
        {
            return;
        }

        if (_methodsByRequestId.Count >= _maximumTrackedRequests)
        {
            _methodsByRequestId.Clear();
        }

        _methodsByRequestId[request.Id.ToString()] = request.Method;
    }

    internal static bool IsDiscoveryMethod(string method)
    {
        return _allowedMethods.Contains(method);
    }

    internal void LogResponse(JsonRpcResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (!_methodsByRequestId.TryRemove(response.Id.ToString(), out string? method))
        {
            return;
        }

        try
        {
            string payload = response.Result?.ToJsonString() ?? "null";
            if (payload.Length > _maximumPayloadLength)
            {
                payload = string.Concat(payload.AsSpan(0, _maximumPayloadLength), "... [truncated]");
            }

            LogDiscoveryResponse(_logger, method, payload);
        }
        catch (JsonException exception)
        {
            LogDiscoveryResponseFailed(_logger, method, exception);
        }
    }

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "MCP discovery response for {Method}: {Payload}")]
    private static partial void LogDiscoveryResponse(ILogger logger, string method, string payload);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message = "Failed to log MCP discovery response for {Method}.")]
    private static partial void LogDiscoveryResponseFailed(ILogger logger, string method, Exception exception);
}
