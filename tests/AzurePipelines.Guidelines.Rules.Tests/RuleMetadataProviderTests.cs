using AzurePipelines.Guidelines.Core;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests;

/// <summary>Tests canonical references resolved from rule metadata.</summary>
public sealed class RuleMetadataProviderTests
{
    [Fact]
    public void GetCanonicalReference_GivenKnownRule_ShouldReturnCanonicalUrl()
    {
        RuleMetadataProvider provider = new();

        string? reference = provider.GetCanonicalReference(new GuidelineId("ADOG-STEPS-001"));

        reference.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GetCanonicalReference_GivenUnknownRule_ShouldReturnNull()
    {
        RuleMetadataProvider provider = new();

        provider.GetCanonicalReference(new GuidelineId("ADOG-STEPS-999")).Should().BeNull();
    }
}
