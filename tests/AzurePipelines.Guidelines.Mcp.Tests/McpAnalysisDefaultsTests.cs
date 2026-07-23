using AzurePipelines.Guidelines.Mcp;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Mcp.Tests;

public sealed class McpAnalysisDefaultsTests
{
    [Fact]
    public void FromConfiguration_GivenEnvironment_ShouldReadAllDefaults()
    {
        McpAnalysisDefaults result = McpAnalysisDefaults.FromConfiguration(
            environment: name => name switch
            {
                McpAnalysisDefaults.GuidelineIdsEnvironmentVariable => "ADOG-STEPS-001",
                McpAnalysisDefaults.CategoryEnvironmentVariable => "steps",
                McpAnalysisDefaults.FormatEnvironmentVariable => "markdown",
                McpAnalysisDefaults.IncludeGuidanceEnvironmentVariable => "yes",
                McpAnalysisDefaults.IncludeHeuristicsEnvironmentVariable => "1",
                _ => null,
            });

        result.GuidelineIds.Should().Be("ADOG-STEPS-001");
        result.Category.Should().Be("steps");
        result.Format.Should().Be("markdown");
        result.IncludeGuidance.Should().BeTrue();
        result.IncludeHeuristics.Should().BeTrue();
    }

    [Fact]
    public void FromConfiguration_GivenCommandLineAndEnvironment_ShouldPreferCommandLine()
    {
        McpAnalysisDefaults result = McpAnalysisDefaults.FromConfiguration(
            ["--category", "jobs", "--format", "compact", "--include-heuristics"],
            name => name == McpAnalysisDefaults.CategoryEnvironmentVariable ? "steps" : "true");

        result.Category.Should().Be("jobs");
        result.Format.Should().Be("compact");
        result.IncludeHeuristics.Should().BeTrue();
    }

    [Fact]
    public void FromConfiguration_GivenNoConfiguration_ShouldUseDocumentedDefaults()
    {
        McpAnalysisDefaults result = McpAnalysisDefaults.FromConfiguration(environment: _ => null);

        result.GuidelineIds.Should().BeNull();
        result.Category.Should().BeNull();
        result.Format.Should().Be("json");
        result.IncludeGuidance.Should().BeFalse();
        result.IncludeHeuristics.Should().BeFalse();
    }

    [Fact]
    public void FromConfiguration_GivenInvalidFormat_ShouldThrow()
    {
        Action act = () => McpAnalysisDefaults.FromConfiguration(
            environment: name => name == McpAnalysisDefaults.FormatEnvironmentVariable ? "xml" : null);

        act.Should().Throw<ArgumentException>().WithMessage("*format*");
    }
}
