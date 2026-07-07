using AzurePipelines.Guidelines.Analysis;
using AzurePipelines.Guidelines.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;

namespace AzurePipelines.Guidelines.Mcp;

/// <summary>
/// DI registration extensions for the MCP server layer.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class GuidelinesMcpServiceCollectionExtensions
{
    /// <summary>
    /// Registers the MCP server, guideline loader, and repository as singletons,
    /// and configures stdio transport so that the process can be used as an MCP server.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="manifestUrl">
    /// Optional override for the guideline manifest URL. When <see langword="null"/>,
    /// <see cref="HttpGuidelineLoader.DefaultManifestUrl"/> is used.
    /// </param>
    /// <returns>The original <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddGuidelinesMcp(
        this IServiceCollection services,
        Uri? manifestUrl = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register the parser, all guideline rules, and the analysis engine.
        services.AddGuidelinesAnalysis();

        // Register the loader as a singleton.
        // and shared for the lifetime of the process (manifest is loaded at startup only).
        services.AddSingleton<IGuidelineLoader>(sp =>
        {
            HttpClient httpClient = new();
            httpClient.DefaultRequestHeaders.Add(
                "User-Agent", "azure-pipelines-guidelines-mcp/1.0");
            return new HttpGuidelineLoader(httpClient, manifestUrl);
        });

        // IGuidelineRepository is a singleton populated synchronously at first resolve.
        // This is safe because resolution happens before the host starts processing
        // MCP requests; there is no concurrent access at that point.
        services.AddSingleton<IGuidelineRepository>(sp =>
        {
            IGuidelineLoader loader = sp.GetRequiredService<IGuidelineLoader>();
            ILogger<GuidelineRepository> logger =
                sp.GetRequiredService<ILogger<GuidelineRepository>>();

            IReadOnlyList<GuidelineDefinition> guidelines;
            try
            {
                guidelines = loader.LoadAsync().GetAwaiter().GetResult();
                LoaderLog.GuidelinesLoaded(logger, guidelines.Count);
            }
            catch (HttpRequestException ex)
            {
                LoaderLog.LoadFailed(logger, ex);
                guidelines = [];
            }
            catch (TaskCanceledException ex)
            {
                LoaderLog.LoadFailed(logger, ex);
                guidelines = [];
            }

            return new GuidelineRepository(guidelines);
        });

        // MCP server: stdio transport + tool and resource discovery from the Mcp assembly.
        services
            .AddMcpServer(options =>
            {
                options.ServerInfo = new Implementation
                {
                    Name = "azure-pipelines-guidelines",
                    Version = "1.0.0",
                };
            })
            .WithStdioServerTransport()
            .WithToolsFromAssembly(typeof(GuidelinesMcpServiceCollectionExtensions).Assembly)
            .WithResourcesFromAssembly(typeof(GuidelinesMcpServiceCollectionExtensions).Assembly);

        return services;
    }
}

/// <summary>High-performance logger messages for the guidelines loader.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
internal static partial class LoaderLog
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Loaded {Count} guideline definitions from manifest.")]
    internal static partial void GuidelinesLoaded(ILogger logger, int count);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Error,
        Message = "Failed to load guideline manifest. The repository will be empty.")]
    internal static partial void LoadFailed(ILogger logger, Exception exception);
}
