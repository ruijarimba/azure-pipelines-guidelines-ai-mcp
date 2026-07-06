namespace AzurePipelines.Guidelines.Core;

/// <summary>Azure Pipelines YAML element that a guideline or detection hint applies to.</summary>
public enum PipelineScope
{
    /// <summary>Applies to a top-level pipeline definition.</summary>
    Pipeline,

    /// <summary>Applies to a stage definition.</summary>
    Stage,

    /// <summary>Applies to a job definition.</summary>
    Job,

    /// <summary>Applies to a step definition.</summary>
    Step,

    /// <summary>Applies to a task.</summary>
    Task,

    /// <summary>Applies to variable definitions.</summary>
    Variables,

    /// <summary>Applies to parameter definitions.</summary>
    Parameters,

    /// <summary>Applies to a template file.</summary>
    Template,

    /// <summary>General guidance not specific to a single YAML element.</summary>
    General,
}
