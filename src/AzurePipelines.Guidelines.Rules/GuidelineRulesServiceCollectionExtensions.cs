using AzurePipelines.Guidelines.Core;
using Microsoft.Extensions.DependencyInjection;

namespace AzurePipelines.Guidelines.Rules;

/// <summary>
/// Extension methods for registering all guideline rules with an
/// <see cref="IServiceCollection"/>.
/// </summary>
public static class GuidelineRulesServiceCollectionExtensions
{
    /// <summary>
    /// Registers every <see cref="IGuidelineRule"/> implementation in this assembly
    /// as a singleton. Rules are resolved via <c>IEnumerable&lt;IGuidelineRule&gt;</c>
    /// by the analysis engine.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddGuidelineRules(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Rules are registered here in ADOG-ID order.
        // Add a new line here for each new rule class.
        services.AddSingleton<IGuidelineRule, RelativeTemplatePathRule>();
        services.AddSingleton<IGuidelineRule, JobMissingCheckoutRule>();
        services.AddSingleton<IGuidelineRule, JobMissingTimeoutRule>();
        services.AddSingleton<IGuidelineRule, ParameterMissingValuesRule>();
        services.AddSingleton<IGuidelineRule, MacroSyntaxInStepsRule>();
        services.AddSingleton<IGuidelineRule, StepMissingTimeoutRule>();
        services.AddSingleton<IGuidelineRule, LargeExpressionInStepsRule>();
        services.AddSingleton<IGuidelineRule, AzureKeyVaultTaskRule>();
        services.AddSingleton<IGuidelineRule, SecretLikeVariableRule>();

        return services;
    }
}
