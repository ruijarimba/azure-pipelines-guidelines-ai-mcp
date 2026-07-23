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

    [Theory]
    [InlineData("condition")]
    [InlineData("continueOnError")]
    [InlineData("enabled")]
    [InlineData("retryCountOnTaskFailure")]
    [InlineData("timeoutInMinutes")]
    public async Task EvaluateAsync_GivenSupportedControlWithoutParameter_ShouldReturnOneDiagnostic(string control)
    {
        string content = $"""
        steps:
          - template: templates/step.yml
            {control}: true
        """;

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(content);

        diagnostics.Should().ContainSingle();
        diagnostics[0].Message.Should().Contain(control);
    }

    [Fact]
    public async Task EvaluateAsync_GivenOneParameterizedAndOneUnparameterizedControl_ShouldReportOnlyUnparameterizedControl()
    {
        const string content = """
        steps:
          - template: templates/step.yml
            condition: ${{ parameters.condition }}
            timeoutInMinutes: 10
            parameters:
              condition: true
        """;

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(content);

        diagnostics.Should().ContainSingle();
        diagnostics[0].Message.Should().Contain("timeoutInMinutes");
        diagnostics[0].Message.Should().NotContain("condition");
    }

    [Fact]
    public async Task EvaluateAsync_GivenDeclaredControlParameterAndUnrelatedLines_ShouldReturnNoDiagnostics()
    {
        const string content = """
        steps:
          - template: templates/step.yml
            parameters:
              condition: true
        # This line is not a control setting.
        """;

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(content);

        diagnostics.Should().BeEmpty();
    }
}
