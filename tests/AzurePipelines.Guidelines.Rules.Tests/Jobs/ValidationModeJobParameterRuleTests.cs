using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Parsing;
using AzurePipelines.Guidelines.Rules.Tests.Fixtures;
using AzurePipelines.Guidelines.Rules.Jobs;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests.Jobs;

/// <summary>Tests validation-mode job parameter configuration.</summary>
public sealed class ValidationModeJobParameterRuleTests
{
    private static readonly YamlPipelineParser _parser = new();
    private static readonly ValidationModeJobParameterRule _rule = new();

    private static async Task<IReadOnlyList<Diagnostic>> EvaluateAsync(string yaml, string filePath = "pipeline.yml")
    {
        PipelineDocument doc = _parser.Parse(yaml, filePath);
        return await _rule.EvaluateAsync(doc).ToListAsync();
    }

    [Fact]
    public void GuidelineId_ShouldBeADOG_JOBS_004()
    {
        // Arrange
        _rule.GuidelineId.Value.Should().Be("ADOG-JOBS-004");
    }

    [Fact]
    public async Task EvaluateAsync_GivenJobWithoutValidationParameter_ShouldReturnOneDiagnostic()
    {
        // Arrange
        string yaml = TestFixtures.Load("Jobs/ValidationModeJobParameter/WithoutValidationParameter.yml");

        // Act
        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        // Assert
        diagnostics.Should().ContainSingle();
        diagnostics[0].GuidelineId.Value.Should().Be("ADOG-JOBS-004");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Warning);
    }

    [Fact]
    public async Task EvaluateAsync_GivenJobWithValidationParameter_ShouldReturnNoDiagnostics()
    {
        // Arrange
        string yaml = TestFixtures.Load("Jobs/ValidationModeJobParameter/WithValidationParameter.yml");

        // Act
        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        // Assert
        diagnostics.Should().BeEmpty();
    }
}
