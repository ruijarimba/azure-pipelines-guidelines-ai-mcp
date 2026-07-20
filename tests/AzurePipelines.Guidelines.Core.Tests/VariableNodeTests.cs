using AzurePipelines.Guidelines.Core;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Core.Tests;

public sealed class VariableNodeTests
{
    [Fact]
    public void Constructor_GivenAllProperties_ShouldExposeThem()
    {
        // Arrange / Act
        VariableNode result = new("Configuration", "Release", true, "shared");

        // Assert
        result.Name.Should().Be("Configuration");
        result.Value.Should().Be("Release");
        result.IsReadOnly.Should().BeTrue();
        result.Group.Should().Be("shared");
    }

    // ── ToString ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ToString_GivenNamedVariable_ShouldShowNameAndValue()
    {
        // Arrange
        VariableNode variable = new(Name: "buildConfiguration", Value: "Release", IsReadOnly: false, Group: null);

        // Act
        string result = variable.ToString();

        // Assert
        result.Should().Be("Variable 'buildConfiguration' = 'Release'");
    }

    [Fact]
    public void ToString_GivenGroupReference_ShouldShowGroupPrefix()
    {
        // Arrange
        VariableNode variable = new(Name: null, Value: null, IsReadOnly: false, Group: "my-variable-group");

        // Act
        string result = variable.ToString();

        // Assert
        result.Should().Be("Variable group:my-variable-group");
    }

    [Fact]
    public void ToString_GivenUnnamedVariableWithNoValue_ShouldUseUnnamed()
    {
        // Arrange
        VariableNode variable = new(Name: null, Value: null, IsReadOnly: false, Group: null);

        // Act
        string result = variable.ToString();

        // Assert
        result.Should().Be("Variable '(unnamed)' = ''");
    }
}
