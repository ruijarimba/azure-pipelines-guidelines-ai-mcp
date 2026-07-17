using AzurePipelines.Guidelines.Cli.Formatters;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Cli.Tests.Formatters;

public sealed class OutputFormatterFactoryTests
{
    [Fact]
    public void SupportedFormats_ShouldContainAllRegisteredFormatters()
    {
        OutputFormatterFactory.SupportedFormats.Should().BeEquivalentTo(
            ["console", "compact", "json", "junit", "sarif", "markdown"]);
    }

    [Theory]
    [InlineData("console")]
    [InlineData("COMPACT")]
    [InlineData("json")]
    [InlineData("junit")]
    [InlineData("sarif")]
    [InlineData("markdown")]
    public void Get_GivenSupportedFormat_ShouldReturnMatchingFormatter(string format)
    {
        ArgumentNullException.ThrowIfNull(format);

        OutputFormatterFactory.Get(format).FormatName.ToUpperInvariant().Should().Be(format.ToUpperInvariant());
        OutputFormatterFactory.IsSupported(format).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Get_GivenBlankFormat_ShouldThrowArgumentException(string? format)
    {
        Action act = () => OutputFormatterFactory.Get(format!);

        act.Should().Throw<ArgumentException>();
        OutputFormatterFactory.IsSupported(format!).Should().BeFalse();
    }

    [Fact]
    public void Get_GivenUnknownFormat_ShouldDescribeSupportedFormats()
    {
        Action act = () => OutputFormatterFactory.Get("xml");

        act.Should().Throw<ArgumentException>().Which.Message.Should().Contain("Supported formats");
    }
}
