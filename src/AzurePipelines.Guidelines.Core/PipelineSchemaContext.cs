namespace AzurePipelines.Guidelines.Core;

/// <summary>
/// Describes the Azure Pipelines YAML template context represented by a document.
/// </summary>
public enum PipelineSchemaContext
{
    /// <summary>A complete pipeline document.</summary>
    Pipeline,

    /// <summary>A stages template document.</summary>
    Stages,

    /// <summary>A jobs template document.</summary>
    Jobs,

    /// <summary>A single job template document.</summary>
    Job,

    /// <summary>A steps template document.</summary>
    Steps,

    /// <summary>A single step template document.</summary>
    Step
}
