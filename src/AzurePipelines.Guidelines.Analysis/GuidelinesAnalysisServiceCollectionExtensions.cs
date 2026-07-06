using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Parsing;
using AzurePipelines.Guidelines.Rules;
using Microsoft.Extensions.DependencyInjection;

namespace AzurePipelines.Guidelines.Analysis;

/// <summary>
/// Extension methods for registering the full analysis stack with an
/// <see cref="IServiceCollection"/>.
/// </summary>
public static class GuidelinesAnalysisServiceCollectionExtensions
{
    /// <summary>
    /// Registers the YAML parser, all guideline rules, and the
    /// <see cref="IPipelineAnalyser"/> implementation.
    /// A single call wires the entire analysis stack; callers only need to
    /// resolve <see cref="IPipelineAnalyser"/> to run an analysis.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddGuidelinesAnalysis(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IPipelineParser, YamlPipelineParser>();
        services.AddGuidelineRules();
        services.AddSingleton<IPipelineAnalyser, PipelineAnalyser>();

        return services;
    }
}
