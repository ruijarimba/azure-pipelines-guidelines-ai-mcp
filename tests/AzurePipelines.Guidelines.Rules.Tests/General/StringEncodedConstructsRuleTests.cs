using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Parsing;
using AzurePipelines.Guidelines.Rules.Tests.Fixtures;
using AzurePipelines.Guidelines.Rules.General;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests.General;

/// <summary>Tests detection of pipeline constructs encoded as strings.</summary>
public sealed class StringEncodedConstructsRuleTests
{
    private static readonly YamlPipelineParser _parser = new();
    private static readonly StringEncodedConstructsRule _rule = new();

    private static async Task<IReadOnlyList<Diagnostic>> EvaluateAsync(string yaml, string filePath = "pipeline.yml")
    {
        PipelineDocument doc = _parser.Parse(yaml, filePath);
        return await _rule.EvaluateAsync(doc).ToListAsync();
    }

    [Fact]
    public void GuidelineId_ShouldBeADOG_GENERAL_002()
    {
        _rule.GuidelineId.Value.Should().Be("ADOG-GENERAL-002");
    }

    [Fact]
    public async Task EvaluateAsync_GivenInlineJsonStyleValue_ShouldReturnOneDiagnostic()
    {
        string yaml = TestFixtures.Load("General/StringEncodedConstructs/InlineJsonStyleValue.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().ContainSingle();
        diagnostics[0].GuidelineId.Value.Should().Be("ADOG-GENERAL-002");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Info);
    }

    [Fact]
    public async Task EvaluateAsync_GivenNativeYamlConstructs_ShouldReturnNoDiagnostics()
    {
        string yaml = TestFixtures.Load("General/StringEncodedConstructs/NativeYamlConstructs.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().BeEmpty();
    }
}
