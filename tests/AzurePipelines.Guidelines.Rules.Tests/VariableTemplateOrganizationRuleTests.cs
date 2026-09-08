using AzurePipelines.Guidelines.Core;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests;

public sealed class VariableTemplateOrganizationRuleTests
{
    private static readonly VariableTemplateOrganizationRule _rule = new();

    private static async Task<IReadOnlyList<Diagnostic>> EvaluateAsync(string content, string filePath = "pipeline.yml")
    {
        PipelineDocument document = new(filePath, content, [], [], [], [], []);
        return await _rule.EvaluateAsync(document).ToListAsync();
    }

    [Fact]
    public void GuidelineId_ShouldBeADOG_VARIABLES_002()
    {
        _rule.GuidelineId.Value.Should().Be("ADOG-VARIABLES-002");
    }

    [Fact]
    public async Task EvaluateAsync_GivenEnvironmentGroupedVariableTemplates_ShouldReturnOneDiagnostic()
    {
        string content = """
        variables:
          - template: templates/vars.yml
        jobs:
          - job: build
            steps:
              - script: echo dev
        """;

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(content);

        diagnostics.Should().ContainSingle();
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Info);
    }

    [Fact]
    public async Task EvaluateAsync_GivenVariableTemplatesWithoutEnvironmentGrouping_ShouldReturnNoDiagnostics()
    {
        string content = """
        variables:
          - template: templates/vars.yml
        jobs:
          - job: build
            steps:
              - script: echo hello
        """;

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(content);

        diagnostics.Should().BeEmpty();
    }
}
