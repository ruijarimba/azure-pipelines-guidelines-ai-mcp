using AzurePipelines.Guidelines.Core;
using Microsoft.Extensions.DependencyInjection;

namespace AzurePipelines.Guidelines.Parsing;

/// <summary>
/// Extension methods for registering the parsing layer into an
/// <see cref="IServiceCollection"/>.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class ParsingServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IPipelineParser"/> as a singleton backed by the
    /// YamlDotNet-based implementation.
    /// </summary>
    public static IServiceCollection AddPipelineParser(this IServiceCollection services)
    {
        services.AddSingleton<IPipelineParser, YamlPipelineParser>();
        return services;
    }
}
