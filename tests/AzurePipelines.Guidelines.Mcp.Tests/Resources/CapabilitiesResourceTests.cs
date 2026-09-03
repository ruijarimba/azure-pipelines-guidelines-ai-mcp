using System.Text.Json;
using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Mcp.Resources;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AzurePipelines.Guidelines.Mcp.Tests.Resources;

public sealed class CapabilitiesResourceTests
{
    [Fact]
    public async Task GetCapabilitiesAsync_ShouldReturnDescriptiveMcpSurface()
    {
        // Arrange
        IGuidelineRepository repository = Substitute.For<IGuidelineRepository>();
        repository.ContentVersion.Returns("abc123");
        CapabilitiesResource sut = new(repository);

        // Act
        string result = await sut.GetCapabilitiesAsync();

        // Assert
        JsonElement payload = JsonSerializer.Deserialize<JsonElement>(result);
        payload.GetProperty("server").GetString().Should().Be("azure-pipelines-guidelines");
        payload.GetProperty("title").GetString().Should().Be("Azure Pipelines YAML Guidelines");
        payload.GetProperty("description").GetString().Should().Contain("Azure Pipelines YAML");
        payload.GetProperty("websiteUrl").GetString()
            .Should().Be("https://github.com/ruijarimba/azure-pipelines-guidelines-ai-mcp");
        payload.GetProperty("version").GetString().Should().Be("1.0.0");
        payload.GetProperty("catalogueVersion").GetString().Should().Be("abc123");

        JsonElement analysisTool = payload.GetProperty("tools")
            .EnumerateArray()
            .Single(item => item.GetProperty("identifier").GetString() == "analyze_template_or_folder");
        analysisTool.GetProperty("title").GetString().Should().Contain("Azure Pipelines YAML");
        analysisTool.GetProperty("description").GetString().Should().Contain("templates");

        payload.GetProperty("resources")
            .EnumerateArray()
            .Select(item => item.GetProperty("identifier").GetString())
            .Should().Contain("adog://capabilities");
        payload.GetProperty("prompts")
            .EnumerateArray()
            .Select(item => item.GetProperty("identifier").GetString())
            .Should().Contain("review");
        payload.GetProperty("supports").GetProperty("automationMetadata").GetBoolean().Should().BeTrue();
        payload.GetProperty("supports").GetProperty("prompts").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetCapabilitiesAsync_ShouldExposeAllStableCapabilityIdentifiers()
    {
        // Arrange
        IGuidelineRepository repository = Substitute.For<IGuidelineRepository>();
        CapabilitiesResource sut = new(repository);

        // Act
        string result = await sut.GetCapabilitiesAsync();

        // Assert
        JsonElement payload = JsonSerializer.Deserialize<JsonElement>(result);
        string?[] toolIdentifiers = [.. payload.GetProperty("tools")
            .EnumerateArray()
            .Select(item => item.GetProperty("identifier").GetString())];
        string?[] resourceIdentifiers = [.. payload.GetProperty("resources")
            .EnumerateArray()
            .Select(item => item.GetProperty("identifier").GetString())];
        string?[] promptIdentifiers = [.. payload.GetProperty("prompts")
            .EnumerateArray()
            .Select(item => item.GetProperty("identifier").GetString())];

        toolIdentifiers.Should().BeEquivalentTo([
            "analyze_template_or_folder", "list_guidelines", "get_guideline", "search_guidelines",
            "list_categories", "explain_diagnostic",
        ]);
        resourceIdentifiers.Should().BeEquivalentTo([
            "adog://capabilities", "adog://guidelines", "adog://guidelines/version",
            "adog://guidelines/category/{category}", "adog://guidelines/{id}",
            "adog://guidelines/{id}/automation",
        ]);
        promptIdentifiers.Should().BeEquivalentTo([
            "review", "review-summary", "review-category", "review-guideline", "explain-guideline",
            "find-guidelines", "list-guidelines", "list-categories",
        ]);
    }
}
