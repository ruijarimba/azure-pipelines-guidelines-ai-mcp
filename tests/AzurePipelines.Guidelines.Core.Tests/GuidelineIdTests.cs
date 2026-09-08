using AzurePipelines.Guidelines.Core;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Core.Tests;

public sealed class GuidelineIdTests
{
    // ── Construction: valid IDs ───────────────────────────────────────────────

    [Theory]
    [InlineData("ADOG-GENERAL-001")]
    [InlineData("ADOG-JOBS-001")]
    [InlineData("ADOG-PARAMETERS-001")]
    [InlineData("ADOG-PIPELINES-001")]
    [InlineData("ADOG-STAGES-001")]
    [InlineData("ADOG-STEPS-001")]
    [InlineData("ADOG-VARIABLES-001")]
    [InlineData("ADOG-STEPS-099")]
    [InlineData("ADOG-STEPS-999")]
    public void Constructor_GivenValidId_ShouldSetValue(string value)
    {
        // Arrange / Act
        GuidelineId id = new(value);

        // Assert
        id.Value.Should().Be(value);
    }

    // ── Construction: invalid IDs ─────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_GivenNullOrWhitespace_ShouldThrowArgumentException(string? value)
    {
        // Arrange / Act
        Action act = () => _ = new GuidelineId(value!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("ADOG-UNKNOWN-001")]    // unrecognised category
    [InlineData("adog-steps-001")]      // wrong case
    [InlineData("ADOG-STEPS-1")]        // too few digits
    [InlineData("ADOG-STEPS-0001")]     // too many digits
    [InlineData("ADOG-STEPS-001-EXTRA")]// extra suffix
    [InlineData("STEPS-001")]           // missing ADOG prefix
    [InlineData("ADOG--001")]           // empty category
    [InlineData("ADOG-STEPS")]          // missing number segment
    public void Constructor_GivenInvalidFormat_ShouldThrowArgumentException(string value)
    {
        // Arrange / Act
        Action act = () => _ = new GuidelineId(value);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    // ── ToString ──────────────────────────────────────────────────────────────

    [Fact]
    public void ToString_GivenValidId_ShouldReturnRawValue()
    {
        // Arrange
        GuidelineId id = new("ADOG-STEPS-001");

        // Act
        string result = id.ToString();

        // Assert
        result.Should().Be("ADOG-STEPS-001");
    }

    // ── Equality ──────────────────────────────────────────────────────────────

    [Fact]
    public void Equals_GivenSameValue_ShouldReturnTrue()
    {
        // Arrange
        GuidelineId a = new("ADOG-STEPS-001");
        GuidelineId b = new("ADOG-STEPS-001");

        // Act / Assert
        a.Equals(b).Should().BeTrue();
        a.Equals((object)b).Should().BeTrue();
    }

    [Fact]
    public void Equals_GivenDifferentValue_ShouldReturnFalse()
    {
        // Arrange
        GuidelineId a = new("ADOG-STEPS-001");
        GuidelineId b = new("ADOG-STEPS-002");

        // Act / Assert
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Equals_GivenNull_ShouldReturnFalse()
    {
        // Arrange
        GuidelineId a = new("ADOG-STEPS-001");

        // Act / Assert
        ((IEquatable<GuidelineId>)a).Equals(null).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_GivenEqualIds_ShouldReturnSameHash()
    {
        // Arrange
        GuidelineId a = new("ADOG-STEPS-001");
        GuidelineId b = new("ADOG-STEPS-001");

        // Act / Assert
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void GetHashCode_GivenDifferentIds_ShouldReturnDifferentHash()
    {
        // Arrange
        GuidelineId a = new("ADOG-STEPS-001");
        GuidelineId b = new("ADOG-STEPS-002");

        // Act / Assert
        a.GetHashCode().Should().NotBe(b.GetHashCode());
    }
}
