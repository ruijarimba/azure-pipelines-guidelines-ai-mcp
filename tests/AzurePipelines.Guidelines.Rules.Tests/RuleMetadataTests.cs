using System.Reflection;
using AzurePipelines.Guidelines.Core;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests;

public sealed class RuleMetadataTests
{
    [Fact]
    public void AllRuleImplementations_ShouldExposeMatchingMetadata()
    {
        Assembly assembly = typeof(IGuidelineRule).Assembly;

        IEnumerable<Type> ruleTypes = assembly
            .GetTypes()
            .Where(t => typeof(IGuidelineRule).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
            .OrderBy(t => t.FullName);

        foreach (Type ruleType in ruleTypes)
        {
            RuleMetadataAttribute? metadata = ruleType.GetCustomAttribute<RuleMetadataAttribute>();
            metadata.Should().NotBeNull($"{ruleType.FullName} should define rule metadata");

            GuidelineId? guidelineId = ruleType
                .GetProperty(nameof(IGuidelineRule.GuidelineId), BindingFlags.Public | BindingFlags.Instance)
                ?.GetValue(Activator.CreateInstance(ruleType)) as GuidelineId;

            guidelineId.Should().NotBeNull($"{ruleType.FullName} should expose a GuidelineId");
            metadata!.RuleId.Should().Be(guidelineId!.Value);
            metadata.GuidelineUrl.Should().NotBeNullOrWhiteSpace();
        }
    }
}
