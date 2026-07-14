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
                guidelines = CanonicalizeReferences(
                    loader.LoadAsync().GetAwaiter().GetResult(),
                    sp.GetRequiredService<IGuidelineMetadataProvider>());
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
                    Version = "1.0.0",
                };
            })
            .WithToolsFromAssembly(typeof(GuidelinesMcpServiceCollectionExtensions).Assembly)
            .WithResourcesFromAssembly(typeof(GuidelinesMcpServiceCollectionExtensions).Assembly);

        return builder;
    }

    /// <summary>
    /// Places canonical rule metadata URLs before distinct manifest references.
    /// </summary>
    /// <param name="guidelines">The definitions loaded from the manifest.</param>
    /// <param name="metadataProvider">The provider of canonical rule URLs.</param>
    /// <returns>Definitions whose references have canonical URLs first.</returns>
    internal static IReadOnlyList<GuidelineDefinition> CanonicalizeReferences(
        IReadOnlyList<GuidelineDefinition> guidelines,
        IGuidelineMetadataProvider metadataProvider)
    {
        List<GuidelineDefinition> canonicalized = new(guidelines.Count);

        foreach (GuidelineDefinition guideline in guidelines)
        {
            string? canonicalReference = metadataProvider.GetCanonicalReference(guideline.Id);
            if (string.IsNullOrWhiteSpace(canonicalReference))
            {
                canonicalized.Add(guideline);
                continue;
            }

            List<string> references = [canonicalReference];
            foreach (string reference in guideline.References)
            {
                if (!string.IsNullOrWhiteSpace(reference) &&
                    !references.Contains(reference, StringComparer.OrdinalIgnoreCase))
                {
                    references.Add(reference);
                }
            }

            canonicalized.Add(guideline with { References = references });
        }

        return canonicalized;
    }
}