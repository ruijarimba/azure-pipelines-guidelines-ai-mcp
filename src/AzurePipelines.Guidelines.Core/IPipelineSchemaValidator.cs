namespace AzurePipelines.Guidelines.Core;

/// <summary>
/// Validates the limited, deterministic local subset of the Azure Pipelines YAML schema.
/// </summary>
public interface IPipelineSchemaValidator
{
    /// <summary>
    /// Validates YAML using the specified template context.
    /// </summary>
    /// <param name="yaml">The YAML content to validate.</param>
    /// <param name="filePath">The source path used in diagnostics.</param>
    /// <param name="context">The expected Azure Pipelines template context.</param>
    /// <returns>Structural diagnostics; an empty collection indicates no findings.</returns>
    public IReadOnlyList<SchemaDiagnostic> Validate(
        string yaml,
        string filePath,
        PipelineSchemaContext context = PipelineSchemaContext.Pipeline);
}
