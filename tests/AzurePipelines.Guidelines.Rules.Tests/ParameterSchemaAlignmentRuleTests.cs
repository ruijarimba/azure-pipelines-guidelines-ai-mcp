using AzurePipelines.Guidelines.Core;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Rules.Tests;

/// <summary>Tests alignment between parameter declarations and their schemas.</summary>
public sealed class ParameterSchemaAlignmentRuleTests
{
    private static readonly ParameterSchemaAlignmentRule _rule = new();

    private static async Task<IReadOnlyList<Diagnostic>> EvaluateAsync(string content, string filePath = "pipeline.yml")
    {
        PipelineDocument document = new(filePath, content, [], [], [], [], []);
        return await _rule.EvaluateAsync(document).ToListAsync();
    }

    [Fact]
    public void GuidelineId_ShouldBeADOG_GENERAL_003()
    {
        _rule.GuidelineId.Value.Should().Be("ADOG-GENERAL-003");
    }

    [Fact]
    public async Task EvaluateAsync_GivenParameterMappedToBooleanFieldWithStringType_ShouldReturnOneDiagnostic()
    {
        string content = """
        parameters:
          - name: condition
            type: string
        """;

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(content);

        diagnostics.Should().ContainSingle();
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Warning);
    }

    [Fact]
    public async Task EvaluateAsync_GivenParameterMappedToBooleanFieldWithBooleanType_ShouldReturnNoDiagnostics()
    {
        string content = """
        parameters:
          - name: continueOnError
            type: boolean
        """;

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(content);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_GivenParameterWithUnmappedName_ShouldReturnNoDiagnostics()
    {
        string content = """
        parameters:
          - name: displayName
            type: string
        """;

        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync(content);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_GivenCommentAndBlankLines_ShouldIgnoreThem()
    {
        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync("""
        parameters:
          # ignored

          - name: displayName
            description: A name
        """);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_GivenInlineParameterType_ShouldUseThatType()
    {
        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync("""
        parameters:
          - name: enabled type: string
        """);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_GivenTypeOnFollowingLine_ShouldDetectMismatch()
    {
        IReadOnlyList<Diagnostic> diagnostics = await EvaluateAsync("""
        parameters:
          - name: enabled
            type: string
        """);

        diagnostics.Should().ContainSingle();
    }
}
