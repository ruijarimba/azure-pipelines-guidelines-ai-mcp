using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Rules.Steps;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests.Steps;

/// <summary>Tests parameter usage in reusable step templates.</summary>
public sealed class StepTemplateParametersRuleTests
{
    private static readonly StepTemplateParametersRule _rule = new();

    private static async Task<IReadOnlyList<Diagnostic>> EvaluateAsync(string content, string filePath = "pipeline.yml")
    {
        PipelineDocument document = new(filePath, content, [], [], [], [], []);
        return await _rule.EvaluateAsync(document).ToListAsync();
    }

    [Fact]
    public void GuidelineId_ShouldBeADOG_STEPS_007()
    {
        _rule.GuidelineId.Value.Should().Be("ADOG-STEPS-007");
    }

    [Fact]
    public async Task EvaluateAsync_GivenReusableStepTemplateWithControlSettingsAndNoParams_ShouldReturnOneDiagnostic()
    {
        string content = """
        steps:
          - template: templates/step.yml
            condition: eq(variables['Build.SourceBranch'], 'refs/heads/main')
        """;

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(content);

        diagnostics.Should().ContainSingle();
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Info);
    }

    [Fact]
    public async Task EvaluateAsync_GivenReusableStepTemplateWithParametersBlock_ShouldReturnNoDiagnostics()
    {
        string content = """
        steps:
          - template: templates/step.yml
            parameters:
              condition: eq(variables['Build.SourceBranch'], 'refs/heads/main')
        """;

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(content);

        diagnostics.Should().BeEmpty();
    }
}
