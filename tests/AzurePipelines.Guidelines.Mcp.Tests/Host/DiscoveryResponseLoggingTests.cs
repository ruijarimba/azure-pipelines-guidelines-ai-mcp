using AzurePipelines.Guidelines.Mcp.Host;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Mcp.Tests.Host;

public sealed class DiscoveryResponseLoggingTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("false")]
    [InlineData("invalid")]
    public void FromValue_WhenValueIsNotTrue_ShouldDisableResponseLogging(string? value)
    {
        DiscoveryResponseLoggingOptions options = DiscoveryResponseLoggingOptions.FromValue(value);

        options.Enabled.Should().BeFalse();
    }

    [Theory]
    [InlineData("true")]
    [InlineData("TRUE")]
    public void FromValue_WhenValueIsTrue_ShouldEnableResponseLogging(string value)
    {
        DiscoveryResponseLoggingOptions options = DiscoveryResponseLoggingOptions.FromValue(value);

        options.Enabled.Should().BeTrue();
    }

    [Theory]
    [InlineData("initialize")]
    [InlineData("tools/list")]
    [InlineData("resources/list")]
    [InlineData("resources/templates/list")]
    [InlineData("prompts/list")]
    public void IsDiscoveryMethod_WhenMethodIsAllowlisted_ShouldReturnTrue(string method)
    {
        DiscoveryResponseLogger.IsDiscoveryMethod(method).Should().BeTrue();
    }

    [Theory]
    [InlineData("tools/call")]
    [InlineData("resources/read")]
    [InlineData("prompts/get")]
    [InlineData("notifications/tools/list_changed")]
    [InlineData("initialize ")]
    [InlineData("TOOLS/LIST")]
    public void IsDiscoveryMethod_WhenMethodIsNotAllowlisted_ShouldReturnFalse(string method)
    {
        DiscoveryResponseLogger.IsDiscoveryMethod(method).Should().BeFalse();
    }
}
