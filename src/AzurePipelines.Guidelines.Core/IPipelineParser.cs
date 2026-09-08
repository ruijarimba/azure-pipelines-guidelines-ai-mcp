namespace AzurePipelines.Guidelines.Core;

/// <summary>
/// Parses raw Azure Pipelines YAML text into a <see cref="PipelineDocument"/> AST.
/// Implementations live in the <c>Parsing</c> project.
/// </summary>
public interface IPipelineParser
{
    /// <summary>
    /// Parses the given <paramref name="yaml"/> text and returns a structured
    /// <see cref="PipelineDocument"/>.
    /// </summary>
    /// <param name="yaml">The raw YAML content to parse.</param>
    /// <param name="filePath">
    /// The file path associated with the content, used in diagnostic messages.
    /// </param>
    /// <returns>A <see cref="PipelineDocument"/> representing the parsed pipeline.</returns>
    /// <exception cref="PipelineParsingException">
    /// Thrown when the YAML cannot be mapped to a valid pipeline document.
    /// </exception>
    public PipelineDocument Parse(string yaml, string filePath);
}
