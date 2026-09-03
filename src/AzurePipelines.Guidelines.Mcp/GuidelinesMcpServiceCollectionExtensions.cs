using AzurePipelines.Guidelines.Analysis;
using AzurePipelines.Guidelines.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace AzurePipelines.Guidelines.Mcp;

/// <summary>
/// DI registration extensions for the MCP server layer.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class GuidelinesMcpServiceCollectionExtensions
{
    /// <summary>
    /// Registers the MCP server, guideline loader, and repository as singletons,
    /// and discovers tools and resources from the Mcp assembly.
    /// The caller must add a transport such as <c>WithStdioServerTransport()</c>
    /// or <c>MapMcp()</c> for SSE mode.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="manifestUrl">
    /// Optional override for the guideline manifest URL. When <see langword="null"/>,
    /// <see cref="HttpGuidelineLoader.DefaultManifestUrl"/> is used.
    /// </param>
    /// <returns>An <see cref="IMcpServerBuilder"/> for configuring the server transport.</returns>
    public static IMcpServerBuilder AddGuidelinesMcp(
        this IServiceCollection services,
        Uri? manifestUrl = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register the parser, all guideline rules, and the analysis engine.
        services.AddGuidelinesAnalysis();

        // The loader is registered as a singleton because the manifest does not change
        // during the lifetime of one process; only one HTTP fetch is needed.
        services.AddSingleton<IGuidelineLoader>(sp =>
        {
            HttpClient httpClient = new();
            httpClient.DefaultRequestHeaders.Add(
                "User-Agent", "azure-pipelines-guidelines-mcp/1.0");
            return new HttpGuidelineLoader(httpClient, manifestUrl);
        });

        // IGuidelineRepository is a singleton populated synchronously at first resolve.
        // This is safe because resolution happens before the host starts processing
        // MCP requests; there is no concurrent access at that point. If the network fetch
        // fails the server starts with an empty guideline list rather than crashing.
        services.AddSingleton<IGuidelineRepository>(sp =>
        {
            IGuidelineLoader loader = sp.GetRequiredService<IGuidelineLoader>();
            ILogger<GuidelineRepository> logger =
                sp.GetRequiredService<ILogger<GuidelineRepository>>();

            IReadOnlyList<GuidelineDefinition> guidelines;
            try
            {
                // Synchronous wait is acceptable here: this factory runs once during
                // service provider build, before any MCP requests are processed.
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

        // MCP server + tool/resource discovery from this assembly.
        // Transport is intentionally left for the host to choose; the library stays
        // usable for both stdio and SSE hosts without a compile-time dependency on either.
        IMcpServerBuilder builder = services
            .AddMcpServer(options =>
            {
                options.ServerInfo = new Implementation
                {
                    Name = "azure-pipelines-guidelines",
                    Title = "Azure Pipelines YAML Guidelines",
                    Version = "0.1.0",
                    Description =
                        "Deterministic analysis and guideline lookup for Azure Pipelines YAML pipelines and reusable templates.",
                    WebsiteUrl = "https://github.com/ruijarimba/azure-pipelines-guidelines-ai-mcp",
                };
            })
            .WithToolsFromAssembly(typeof(GuidelinesMcpServiceCollectionExtensions).Assembly)
            .WithResourcesFromAssembly(typeof(GuidelinesMcpServiceCollectionExtensions).Assembly)
            .WithPromptsFromAssembly(typeof(GuidelinesMcpServiceCollectionExtensions).Assembly);

        return builder;
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
