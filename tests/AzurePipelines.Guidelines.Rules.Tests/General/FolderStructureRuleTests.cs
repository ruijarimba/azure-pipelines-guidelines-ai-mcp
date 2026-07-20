using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Parsing;
using AzurePipelines.Guidelines.Rules.Tests.Fixtures;
using AzurePipelines.Guidelines.Rules.General;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests.General;

/// <summary>Tests detection of template references that use ad-hoc folder paths.</summary>
public sealed class FolderStructureRuleTests
{
    private static readonly YamlPipelineParser _parser = new();
    private static readonly FolderStructureRule _rule = new();

    private static async Task<IReadOnlyList<Diagnostic>> EvaluateAsync(string yaml, string filePath = "pipeline.yml")
    {
        PipelineDocument doc = _parser.Parse(yaml, filePath);
        return await _rule.EvaluateAsync(doc).ToListAsync();
    }

    [Fact]
    public void GuidelineId_ShouldBeADOG_GENERAL_005()
    {
        _rule.GuidelineId.Value.Should().Be("ADOG-GENERAL-005");
    }

    [Fact]
    public async Task EvaluateAsync_GivenAdHocTemplatePath_ShouldReturnOneDiagnostic()
    {
        string yaml = TestFixtures.Load("General/FolderStructure/WithAdHocPath.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().ContainSingle();
        diagnostics[0].GuidelineId.Value.Should().Be("ADOG-GENERAL-005");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task EvaluateAsync_GivenSharedRootTemplatePath_ShouldReturnNoDiagnostics()
    {
        string yaml = TestFixtures.Load("General/FolderStructure/WithSharedRootPath.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_GivenAbsoluteTemplatePath_ShouldReturnNoDiagnostics()
    {
        string yaml = TestFixtures.Load("General/FolderStructure/WithAbsolutePath.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().BeEmpty();
    }
}
