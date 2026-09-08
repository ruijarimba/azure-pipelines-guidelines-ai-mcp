using AzurePipelines.Guidelines.Core;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AzurePipelines.Guidelines.Integration.Tests;

public sealed class RegisteredGuidelineCoverageTests
{
    [Fact]
    public void RepositoryExpectations_ShouldCoverEveryRegisteredGuideline()
    {
        PipelineRepositoryIntegrationTestsBase[] repositories =
        [
            new DockerPipelineRepositoryTests(),
            new HelmPipelineRepositoryTests(),
            new TerraformPipelineRepositoryTests(),
        ];

        HashSet<GuidelineId> expectedIds = [.. repositories
            .SelectMany(repository => repository.GetExpectedGuidelineIds())];

        using ServiceProvider provider = PipelineRepositoryIntegrationTestsBase.CreateServiceProvider();
        HashSet<GuidelineId> registeredIds = [.. provider
            .GetServices<IGuidelineRule>()
            .Select(rule => rule.GuidelineId)];

        expectedIds.Should().BeEquivalentTo(registeredIds);
    }
}
