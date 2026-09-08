using AzurePipelines.Guidelines.Core;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests;

public sealed class ReusableJobTemplateParametersRuleTests
{
    private static readonly ReusableJobTemplateParametersRule _rule = new();

    private static async Task<IReadOnlyList<Diagnostic>> EvaluateAsync(string content, string filePath = "pipeline.yml")
    {
        PipelineDocument document = new(filePath, content, [], [], [], [], []);
        return await _rule.EvaluateAsync(document).ToListAsync();
    }

    [Fact]
    public void GuidelineId_ShouldBeADOG_JOBS_005()
    {
        _rule.GuidelineId.Value.Should().Be("ADOG-JOBS-005");
    }

    [Fact]
    public async Task EvaluateAsync_GivenReusableJobTemplateWithControlSettingsAndNoParams_ShouldReturnOneDiagnostic()
    {
        string content = """
        jobs:
          - template: templates/job.yml
            dependsOn: build
        """;

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(content);

        diagnostics.Should().ContainSingle();
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Warning);
    }

    [Fact]
    public async Task EvaluateAsync_GivenReusableJobTemplateWithParametersBlock_ShouldReturnNoDiagnostics()
    {
        string content = """
        jobs:
          - template: templates/job.yml
            parameters:
              pool: vmImage:ubuntu-latest
        """;

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(content);

        diagnostics.Should().BeEmpty();
    }
}
