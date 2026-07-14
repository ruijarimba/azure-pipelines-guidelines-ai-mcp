using AzurePipelines.Guidelines.Core;
using Microsoft.Extensions.DependencyInjection;

namespace AzurePipelines.Guidelines.Rules;

/// <summary>
/// Extension methods for registering all guideline rules with an
/// <see cref="IServiceCollection"/>.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
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

        services.AddSingleton<IGuidelineMetadataProvider, RuleMetadataProvider>();

        // Rules are registered here in ADOG-ID order.
        // Add a new line here for each new rule class.
        services.AddSingleton<IGuidelineRule, RelativeTemplatePathRule>();
        services.AddSingleton<IGuidelineRule, FolderStructureRule>();
        services.AddSingleton<IGuidelineRule, JobMissingCheckoutRule>();
        services.AddSingleton<IGuidelineRule, MultipleStepsTemplatesInJobRule>();
        services.AddSingleton<IGuidelineRule, JobMissingTimeoutRule>();
        services.AddSingleton<IGuidelineRule, ParameterMissingValuesRule>();
        services.AddSingleton<IGuidelineRule, MacroSyntaxInStepsRule>();
        services.AddSingleton<IGuidelineRule, PipelineDocumentationRule>();
        services.AddSingleton<IGuidelineRule, ReadonlyVariableRule>();
        services.AddSingleton<IGuidelineRule, JobLevelVariableRule>();
        services.AddSingleton<IGuidelineRule, StringEncodedConstructsRule>();
        services.AddSingleton<IGuidelineRule, StepRetryRule>();
        services.AddSingleton<IGuidelineRule, TaskEnvironmentVariablesRule>();
        services.AddSingleton<IGuidelineRule, PipelineValidationModeRule>();
        services.AddSingleton<IGuidelineRule, ServiceConnectionAuthRule>();
        services.AddSingleton<IGuidelineRule, InlineTemplateLogicRule>();
        services.AddSingleton<IGuidelineRule, ValidationModeJobParameterRule>();
        services.AddSingleton<IGuidelineRule, ReusableJobTemplateParametersRule>();
        services.AddSingleton<IGuidelineRule, DiagnosticLoggingRule>();
        services.AddSingleton<IGuidelineRule, DiagnosticLoggingConsiderationRule>();
        services.AddSingleton<IGuidelineRule, SingleResponsibilityJobRule>();
        services.AddSingleton<IGuidelineRule, StepParameterValidationRule>();
        services.AddSingleton<IGuidelineRule, SeparateConfigurationRule>();
        services.AddSingleton<IGuidelineRule, StepTemplateParametersRule>();
        services.AddSingleton<IGuidelineRule, ParameterSchemaAlignmentRule>();
        services.AddSingleton<IGuidelineRule, VariableTemplateOrganizationRule>();
        services.AddSingleton<IGuidelineRule, HardCodedValuesRule>();
        services.AddSingleton<IGuidelineRule, MultiEnvironmentVariableTemplateRule>();
        services.AddSingleton<IGuidelineRule, ParameterGroupingRule>();
        services.AddSingleton<IGuidelineRule, VariableScopeRule>();
        services.AddSingleton<IGuidelineRule, StepMissingTimeoutRule>();
        services.AddSingleton<IGuidelineRule, LargeExpressionInStepsRule>();
        services.AddSingleton<IGuidelineRule, AzureKeyVaultTaskRule>();
        services.AddSingleton<IGuidelineRule, SecretLikeVariableRule>();
        services.AddSingleton<IGuidelineRule, EnvironmentParameterMinimizationRule>();
        services.AddSingleton<IGuidelineRule, UseStagesForRelatedJobsRule>();
        services.AddSingleton<IGuidelineRule, RunIndependentStagesInParallelRule>();

        return services;
    }
}
