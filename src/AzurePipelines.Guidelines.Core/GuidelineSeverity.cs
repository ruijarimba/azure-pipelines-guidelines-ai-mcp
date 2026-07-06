namespace AzurePipelines.Guidelines.Core;

/// <summary>Strength of an Azure Pipelines guideline recommendation.</summary>
public enum GuidelineSeverity
{
    /// <summary>You should almost always follow this recommendation.</summary>
    Do,

    /// <summary>You should almost never do this.</summary>
    DoNot,

    /// <summary>Generally not a good idea, but breaking the rule sometimes makes sense.</summary>
    Avoid,

    /// <summary>Should generally be followed, but legitimate exceptions exist.</summary>
    Consider,
}
