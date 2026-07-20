using AzurePipelines.Guidelines.Core;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Core.Tests;

public sealed class StepNodeTests
{
    [Fact]
    public void Constructor_GivenAllProperties_ShouldExposeThem()
    {
        // Arrange / Act
        StepNode result = new("Task@1", "echo test", "Run task", 5, false, "always()", 3);

        // Assert
        result.Task.Should().Be("Task@1");
        result.Script.Should().Be("echo test");
        result.DisplayName.Should().Be("Run task");
        result.TimeoutInMinutes.Should().Be(5);
        result.IsCheckout.Should().BeFalse();
        result.Condition.Should().Be("always()");
        result.Line.Should().Be(3);
    }

    // ── ToString ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ToString_GivenCheckoutStep_ShouldShowCheckoutKind()
    {
        // Arrange
        StepNode step = new(Task: null, Script: null, DisplayName: "Checkout sources",
            TimeoutInMinutes: null, IsCheckout: true, Condition: null, Line: 5);

        // Act
        string result = step.ToString();

        // Assert
        result.Should().Be("Step checkout 'Checkout sources' (line 5)");
    }

    [Fact]
    public void ToString_GivenTaskStep_ShouldShowTaskIdentifier()
    {
        // Arrange
        StepNode step = new(Task: "AzureCLI@2", Script: null, DisplayName: "Run Azure CLI",
            TimeoutInMinutes: null, IsCheckout: false, Condition: null, Line: 20);

        // Act
        string result = step.ToString();

        // Assert
        result.Should().Be("Step AzureCLI@2 'Run Azure CLI' (line 20)");
    }

    [Fact]
    public void ToString_GivenScriptStep_ShouldShowScriptKind()
    {
        // Arrange
        StepNode step = new(Task: null, Script: "echo hello", DisplayName: "Say hello",
            TimeoutInMinutes: null, IsCheckout: false, Condition: null, Line: 30);

        // Act
        string result = step.ToString();

        // Assert
        result.Should().Be("Step script 'Say hello' (line 30)");
    }

    [Fact]
    public void ToString_GivenUnnamedStepWithNoLine_ShouldUseUnnamedAndQuestionMark()
    {
        // Arrange
        StepNode step = new(Task: "UseDotNet@2", Script: null, DisplayName: null,
            TimeoutInMinutes: null, IsCheckout: false, Condition: null, Line: null);

        // Act
        string result = step.ToString();

        // Assert
        result.Should().Be("Step UseDotNet@2 '(unnamed)' (line ?)");
    }
}
