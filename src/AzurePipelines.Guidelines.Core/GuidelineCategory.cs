namespace AzurePipelines.Guidelines.Core;

/// <summary>Category of an Azure Pipelines guideline.</summary>
public enum GuidelineCategory
{
    /// <summary>General recommendations that apply to more than one category.</summary>
    General,

    /// <summary>Jobs-related recommendations.</summary>
    Jobs,

    /// <summary>Parameters-related recommendations.</summary>
    Parameters,

    /// <summary>Pipelines-related recommendations.</summary>
    Pipelines,

    /// <summary>Stages-related recommendations.</summary>
    Stages,

    /// <summary>Steps-related recommendations.</summary>
    Steps,

    /// <summary>Variables-related recommendations.</summary>
    Variables,
}
