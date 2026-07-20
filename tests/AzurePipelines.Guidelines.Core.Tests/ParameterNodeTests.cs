using AzurePipelines.Guidelines.Core;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Core.Tests;

public sealed class ParameterNodeTests
{
    [Fact]
    public void Constructor_GivenAllProperties_ShouldExposeThem()
    {
        // Arrange / Act
        ParameterNode result = new("environment", "string", "dev", ["dev", "prod"]);

        // Assert
        result.Name.Should().Be("environment");
        result.Type.Should().Be("string");
        result.Default.Should().Be("dev");
        result.Values.Should().BeEquivalentTo("dev", "prod");
    }

    // ── ToString ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ToString_GivenParameterWithExplicitTypeAndNoAllowedValues_ShouldOmitValueCount()
    {
        // Arrange
        ParameterNode parameter = new("environment", Type: "string", Default: "dev", Values: []);

        // Act
        string result = parameter.ToString();

        // Assert
        result.Should().Be("Parameter 'environment' (type: string)");
    }

    [Fact]
    public void ToString_GivenParameterWithNullType_ShouldDefaultToString()
    {
        // Arrange
        ParameterNode parameter = new("debug", Type: null, Default: null, Values: []);

        // Act
        string result = parameter.ToString();

        // Assert
        result.Should().Be("Parameter 'debug' (type: string)");
    }

    [Fact]
    public void ToString_GivenParameterWithAllowedValues_ShouldIncludeValueCount()
    {
        // Arrange
        ParameterNode parameter = new("region", Type: "string", Default: "eastus",
            Values: ["eastus", "westeurope", "southeastasia"]);

        // Act
        string result = parameter.ToString();

        // Assert
        result.Should().Be("Parameter 'region' (type: string, 3 values)");
    }
}
