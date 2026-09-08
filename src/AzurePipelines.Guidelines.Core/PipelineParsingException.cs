namespace AzurePipelines.Guidelines.Core;

/// <summary>
/// The exception that is thrown when an Azure Pipelines YAML document cannot be
/// parsed into a valid <see cref="PipelineDocument"/>.
/// </summary>
public sealed class PipelineParsingException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="PipelineParsingException"/>.
    /// </summary>
    public PipelineParsingException()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="PipelineParsingException"/> with a
    /// descriptive message.
    /// </summary>
    public PipelineParsingException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="PipelineParsingException"/> with a
    /// descriptive message and an inner exception.
    /// </summary>
    public PipelineParsingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
