namespace AzurePipelines.Guidelines.Core;

/// <summary>
/// Attaches stable rule metadata to a guideline rule implementation.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class RuleMetadataAttribute : Attribute
{
    /// <summary>
    /// Initialises the metadata for a rule implementation.
    /// </summary>
    /// <param name="ruleId">The stable guideline identifier, for example <c>ADOG-STEPS-001</c>.</param>
    /// <param name="guidelineUrl">The canonical GitHub URL for the guideline markdown.</param>
    public RuleMetadataAttribute(string ruleId, string guidelineUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ruleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(guidelineUrl);

        RuleId = ruleId;
        GuidelineUrl = guidelineUrl;
    }

    /// <summary>Gets the stable guideline identifier.</summary>
    public string RuleId
    {
        get;
    }

    /// <summary>Gets the canonical GitHub URL for the guideline markdown.</summary>
    public string GuidelineUrl
    {
        get;
    }
}
