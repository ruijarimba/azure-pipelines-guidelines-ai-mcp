using System.Reflection;
using AzurePipelines.Guidelines.Core;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests;

/// <summary>Guards the one-to-one inventory of rule implementations and dedicated test classes.</summary>
public sealed class RuleCoverageInventoryTests
{
    [Fact]
    public void EveryRuleImplementation_ShouldHaveDedicatedBehavioralTestClass()
    {
        Assembly rulesAssembly = typeof(RelativeTemplatePathRule).Assembly;
        Assembly testsAssembly = typeof(RuleCoverageInventoryTests).Assembly;

        Type[] ruleTypes = [..
            rulesAssembly.GetTypes()
                .Where(type => typeof(IGuidelineRule).IsAssignableFrom(type))
                .Where(type => type is { IsClass: true, IsAbstract: false })
                .OrderBy(type => type.FullName)];

        Type[] testTypes = [..
            testsAssembly.GetTypes()
                .Where(type => type is { IsClass: true, IsAbstract: false })
                .Where(type => type.Name.EndsWith("RuleTests", StringComparison.Ordinal))];

        ruleTypes.Should().NotBeEmpty();
        testTypes.Should().NotBeEmpty();

        foreach (Type ruleType in ruleTypes)
        {
            testTypes.Should().Contain(
                testType => testType.Name.Equals(ruleType.Name + "Tests", StringComparison.Ordinal),
                $"{ruleType.Name} must have a dedicated test class with positive and compliant behavior coverage");
        }
    }
}
