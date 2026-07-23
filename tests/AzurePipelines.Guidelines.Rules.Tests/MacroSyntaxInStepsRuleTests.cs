using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Parsing;
using AzurePipelines.Guidelines.Rules.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests;

public sealed class MacroSyntaxInStepsRuleTests
{
    private static readonly YamlPipelineParser _parser = new();
    private static readonly MacroSyntaxInStepsRule _rule = new();

    private static async Task<IReadOnlyList<Diagnostic>> EvaluateAsync(string yaml, string filePath = "pipeline.yml")
    {
        PipelineDocument doc = _parser.Parse(yaml, filePath);
        return await _rule.EvaluateAsync(doc).ToListAsync();
    }

    // ── GuidelineId ───────────────────────────────────────────────────────────

    [Fact]
    public void GuidelineId_ShouldBeADOG_STEPS_001()
    {
        _rule.GuidelineId.Value.Should().Be("ADOG-STEPS-001");
    }

    // ── Violations ────────────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_GivenMacroSyntax_ShouldReturnDiagnostic()
    {
        string yaml = TestFixtures.Load("MacroSyntax/WithMacroSyntax.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().ContainSingle();
        diagnostics[0].GuidelineId.Value.Should().Be("ADOG-STEPS-001");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Warning);
        diagnostics[0].Message.Should().Contain("$(BUILD_CONFIGURATION)");
    }

    // ── No violations ─────────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_GivenRuntimeExpression_ShouldReturnNoDiagnostics()
    {
        string yaml = TestFixtures.Load("MacroSyntax/WithRuntimeExpression.yml");

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_GivenTemplateParameterMacros_ShouldReturnNoDiagnostics()
    {
        const string yaml = """
            extends:
              template: /pipelines/templates/base.yaml
              parameters:
                agentPool: $(agentPool)
                configuration: ${{ variables.configuration }}
            stages:
              - template: /pipelines/stages/build-stage.yaml
                parameters:
                  environment: $(environment)
            jobs:
              - template: /pipelines/jobs/build-job.yaml
                parameters:
                  imageName: $(imageName)
            """;

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_GivenMacroOutsideTemplateParameters_ShouldReturnDiagnostic()
    {
        const string yaml = """
            name: build-$(buildNumber)
            jobs:
              - template: /pipelines/jobs/build-job.yaml
                parameters:
                  imageName: $(imageName)
            """;

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().ContainSingle();
        diagnostics[0].Message.Should().Contain("$(buildNumber)");
    }

    [Fact]
    public async Task EvaluateAsync_GivenCommentedMacro_ShouldReturnNoDiagnostics()
    {
        const string yaml = """
            # $(yamlComment)
            steps:
              - script: |
                  # $(scriptComment)
                  echo no macro here
            """;

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(yaml);

        diagnostics.Should().BeEmpty();
    }
}
