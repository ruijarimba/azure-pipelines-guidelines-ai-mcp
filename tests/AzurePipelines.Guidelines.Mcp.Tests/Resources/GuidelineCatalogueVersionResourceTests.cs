using System.Text.Json;
using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Mcp.Resources;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AzurePipelines.Guidelines.Mcp.Tests.Resources;

public sealed class GuidelineCatalogueVersionResourceTests
{
    [Fact]
    public async Task GetCatalogueVersionAsync_ShouldReturnRepositoryVersion()
    {
        // Arrange
        IGuidelineRepository repository = Substitute.For<IGuidelineRepository>();
        repository.ContentVersion.Returns("abc123");
        GuidelineCatalogueVersionResource sut = new(repository);

        // Act
        string result = await sut.GetCatalogueVersionAsync();

        // Assert
        JsonElement payload = JsonSerializer.Deserialize<JsonElement>(result);
        payload.GetProperty("version").GetString().Should().Be("abc123");
    }
}
