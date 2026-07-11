using AzurePipelines.Guidelines.Core;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Core.Tests;

public sealed class PipelineParsingExceptionTests
{
    [Fact]
    public void DefaultConstructor_ShouldCreateExceptionWithDefaultMessage()
    {
        PipelineParsingException exception = new();

        exception.Message.Should().Contain("PipelineParsingException");
    }

    [Fact]
    public void MessageConstructor_ShouldCaptureProvidedMessage()
    {
        PipelineParsingException exception = new("Unable to parse pipeline");

        exception.Message.Should().Be("Unable to parse pipeline");
    }

    [Fact]
    public void MessageAndInnerExceptionConstructor_ShouldCaptureBoth()
    {
        InvalidOperationException innerException = new("inner failure");

        PipelineParsingException exception = new("outer failure", innerException);

        exception.Message.Should().Be("outer failure");
        exception.InnerException.Should().BeSameAs(innerException);
    }
}
