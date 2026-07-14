using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Parsing;
using AzurePipelines.Guidelines.Rules.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests;

/// <summary>Tests authentication configuration for service connections.</summary>
public sealed class ServiceConnectionAuthRuleTests
{
    private static readonly YamlPipelineParser _parser = new();
    private static readonly ServiceConnectionAuthRule _rule = new();

    private static async Task<IReadOnlyList<Diagnostic>> EvaluateAsync(string yaml, string filePath = "pipeline.yml")
    {
        PipelineDocument doc = _parser.Parse(yaml, filePath);
        return await _rule.EvaluateAsync(doc).ToListAsync();
    }

    [Fact]
    public void GuidelineId_ShouldBeADOG_STEPS_008()
    {
        _rule.GuidelineId.Value.Should().Be("ADOG-STEPS-008");
    }

    [Fact]
    public async Task EvaluateAsync_GivenExternalStepWithoutServiceConnection_ShouldReturnOneDiagnostic()
    {
        string yaml = TestFixtures.Load("ServiceConnectionAuth/WithoutServiceConnection.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().ContainSingle();
        diagnostics[0].GuidelineId.Value.Should().Be("ADOG-STEPS-008");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task EvaluateAsync_GivenExternalStepWithServiceConnection_ShouldReturnNoDiagnostics()
    {
        string yaml = TestFixtures.Load("ServiceConnectionAuth/WithServiceConnection.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().BeEmpty();
    }
}
