using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Parsing;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Parsing.Tests;

public sealed class CommentFreeContentBuilderTests
{
    private static readonly YamlPipelineParser _parser = new();

    [Fact]
    public void Parse_GivenYamlComments_ShouldMaskCommentsAndPreserveQuotedHashes()
    {
        const string yaml = """
            # $(commented)
            name: "value # $(quoted)"
            value: active # $(inlineComment)
            """;

        PipelineDocument document = _parser.Parse(yaml, "pipeline.yml");

        document.CommentFreeContent.Length.Should().Be(yaml.Length);
        document.CommentFreeContent.Should().NotContain("$(commented)");
        document.CommentFreeContent.Should().NotContain("$(inlineComment)");
        document.CommentFreeContent.Should().Contain("value # $(quoted)");
    }

    [Fact]
    public void Parse_GivenBlockScalarComment_ShouldMaskCommentOnlyScriptLines()
    {
        const string yaml = """
            steps:
              - script: |
                  # $(scriptComment)
                  echo no macro here
            """;

        PipelineDocument document = _parser.Parse(yaml, "pipeline.yml");

        document.CommentFreeContent.Length.Should().Be(yaml.Length);
        document.CommentFreeContent.Should().NotContain("$(scriptComment)");
        document.CommentFreeContent.Should().Contain("echo no macro here");
    }
}
