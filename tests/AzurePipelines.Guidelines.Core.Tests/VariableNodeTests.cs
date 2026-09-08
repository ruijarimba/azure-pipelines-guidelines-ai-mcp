using AzurePipelines.Guidelines.Core;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Core.Tests;

public sealed class VariableNodeTests
{
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
