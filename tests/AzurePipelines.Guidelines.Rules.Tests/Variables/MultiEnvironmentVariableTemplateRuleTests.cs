using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Parsing;
using AzurePipelines.Guidelines.Rules.Tests.Fixtures;
using AzurePipelines.Guidelines.Rules.Variables;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests.Variables;

/// <summary>Tests detection of environment variables spread across multiple templates.</summary>
public sealed class MultiEnvironmentVariableTemplateRuleTests
{
    private static readonly YamlPipelineParser _parser = new();
    private static readonly MultiEnvironmentVariableTemplateRule _rule = new();

    private static async Task<IReadOnlyList<Diagnostic>> EvaluateAsync(string yaml, string filePath = "pipeline.yml")
    {
        PipelineDocument doc = _parser.Parse(yaml, filePath);
        return await _rule.EvaluateAsync(doc).ToListAsync();
    }

    [Fact]
    public void GuidelineId_ShouldBeADOG_VARIABLES_006()
    {
        _rule.GuidelineId.Value.Should().Be("ADOG-VARIABLES-006");
    }

    [Fact]
    public async Task EvaluateAsync_GivenMultiEnvironmentVariables_ShouldReturnOneDiagnostic()
    {
        string yaml = TestFixtures.Load("Variables/MultiEnvironmentVariableTemplate/MultiEnvironment.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().ContainSingle();
        diagnostics[0].GuidelineId.Value.Should().Be("ADOG-VARIABLES-006");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task EvaluateAsync_GivenSingleEnvironmentVariables_ShouldReturnNoDiagnostics()
    {
        string yaml = TestFixtures.Load("Variables/MultiEnvironmentVariableTemplate/SingleEnvironment.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().BeEmpty();
    }
}
