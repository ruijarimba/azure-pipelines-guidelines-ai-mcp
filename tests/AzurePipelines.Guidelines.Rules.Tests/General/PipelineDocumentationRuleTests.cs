using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Parsing;
using AzurePipelines.Guidelines.Rules.Tests.Fixtures;
using AzurePipelines.Guidelines.Rules.General;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests.General;

/// <summary>Tests detection of pipelines that lack useful documentation.</summary>
public sealed class PipelineDocumentationRuleTests
{
    private static readonly YamlPipelineParser _parser = new();
    private static readonly PipelineDocumentationRule _rule = new();

    private static async Task<IReadOnlyList<Diagnostic>> EvaluateAsync(string yaml, string filePath = "pipeline.yml")
    {
        PipelineDocument doc = _parser.Parse(yaml, filePath);
        return await _rule.EvaluateAsync(doc).ToListAsync();
    }

    [Fact]
    public void GuidelineId_ShouldBeADOG_GENERAL_004()
    {
        _rule.GuidelineId.Value.Should().Be("ADOG-GENERAL-004");
    }

    [Fact]
    public async Task EvaluateAsync_GivenPipelineWithoutHeaderComment_ShouldReturnOneDiagnostic()
    {
        string yaml = TestFixtures.Load("General/PipelineDocumentation/WithoutHeaderComment.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().ContainSingle();
        diagnostics[0].GuidelineId.Value.Should().Be("ADOG-GENERAL-004");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task EvaluateAsync_GivenPipelineWithHeaderComment_ShouldReturnNoDiagnostics()
    {
        string yaml = TestFixtures.Load("General/PipelineDocumentation/WithHeaderComment.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().BeEmpty();
    }
}
