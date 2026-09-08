using System.ComponentModel;
using System.Reflection;
using AzurePipelines.Guidelines.Mcp.Tools;
using FluentAssertions;
using ModelContextProtocol.Server;
using Xunit;

namespace AzurePipelines.Guidelines.Mcp.Tests.Tools;

public sealed class AnalyzeTemplateOrFolderToolMetadataTests
{
    [Fact]
    public void AnalyzeTemplateAsync_Metadata_ShouldIdentifyAzurePipelinesTemplateGuidelines()
    {
        // Arrange
        MethodInfo? method = typeof(AnalyzeTemplateOrFolderTool).GetMethod(
            "AnalyzeTemplateAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);

        // Act
        McpServerToolAttribute? tool = method?.GetCustomAttribute<McpServerToolAttribute>();
        DescriptionAttribute? description = method?.GetCustomAttribute<DescriptionAttribute>();

        // Assert
        tool?.Name.Should().Be("analyze_template_or_folder");
        tool?.Title.Should().Be("Analyze Azure Pipelines YAML pipelines and templates");
        description?.Description.Should().Contain("Azure Pipelines YAML");
        description?.Description.Should().Contain("reusable step, job, stage, or variable templates");
    }
}
