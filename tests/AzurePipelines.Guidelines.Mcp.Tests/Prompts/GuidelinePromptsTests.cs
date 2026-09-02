using AzurePipelines.Guidelines.Mcp.Prompts;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Mcp.Tests.Prompts;

public sealed class GuidelinePromptsTests
{
    private static readonly string[] _recommendationPrompts =
    [
        GuidelinePrompts.Review("steps:\n- script: dotnet build"),
        GuidelinePrompts.Review("steps:\n- script: dotnet build"),
        GuidelinePrompts.ReviewSummary(),
        GuidelinePrompts.ReviewCategory("steps", "pipelines"),
        GuidelinePrompts.ReviewGuidelines("ADOG-STEPS-001,ADOG-JOBS-006", "pipeline.yml"),
        GuidelinePrompts.ExplainGuideline("ADOG-STEPS-001"),
        GuidelinePrompts.FindGuidelines("template", "steps"),
        GuidelinePrompts.ListGuidelines("steps"),
    ];

    [Fact]
    public void Review_GivenInlineYaml_ShouldSelectTemplateAnalysisTool()
    {
        string result = GuidelinePrompts.Review("steps:\n- script: dotnet build");

        result.Should().Contain("analyze_template_or_folder");
        result.Should().Contain("yaml for inline content");
        result.Should().Contain("DO, DO-NOT, AVOID, and CONSIDER");
        result.Should().Contain("Do not modify files");
    }

    [Fact]
    public void ReviewSummary_ShouldRequestRepositoryWideTableSummary()
    {
        string result = GuidelinePrompts.ReviewSummary();

        result.Should().Contain("fileOrPath=\".\"");
        result.Should().Contain("summaryMode=true");
        result.Should().Contain("Markdown table");
        result.Should().Contain("Rule ID, Recommendation, Category, Occurrences, and Files");
        result.Should().Contain("Do not modify files");
    }

    [Fact]
    public void Review_GivenFileOrDirectoryPath_ShouldSelectTemplateAnalysisTool()
    {
        string result = GuidelinePrompts.Review("pipelines/build.yml");

        result.Should().Contain("analyze_template_or_folder");
        result.Should().Contain("file or directory path");
        result.Should().Contain("DO, DO-NOT, AVOID, and CONSIDER");
        result.Should().Contain("Do not modify files");
    }

    [Fact]
    public void Review_GivenNoFileOrPath_ShouldRequestAutomaticResolution()
    {
        string result = GuidelinePrompts.Review();

        result.Should().Contain("automatic repository path resolution");
        result.Should().Contain("analyze_template_or_folder");
        result.Should().Contain("Do not modify files");
    }

    [Fact]
    public void ReviewCategory_GivenCategory_ShouldIncludeTargetAndCategory()
    {
        string result = GuidelinePrompts.ReviewCategory("steps", "pipelines");

        result.Should().Contain("Target: pipelines");
        result.Should().Contain("Category: steps");
        result.Should().Contain("analyze_template_or_folder");
        result.Should().Contain("DO, DO-NOT, AVOID, and CONSIDER");
    }

    [Fact]
    public void ReviewCategory_GivenNoFileOrPath_ShouldRequestAutomaticResolution()
    {
        string result = GuidelinePrompts.ReviewCategory("steps");

        result.Should().Contain("Target: (automatic repository path resolution)");
        result.Should().Contain("Category: steps");
        result.Should().Contain("analyze_template_or_folder");
    }

    [Fact]
    public void ReviewGuidelines_GivenGuidelineIds_ShouldIncludeTargetAndIds()
    {
        string result = GuidelinePrompts.ReviewGuidelines("ADOG-STEPS-001,ADOG-JOBS-006", "pipeline.yml");

        result.Should().Contain("Guideline IDs: ADOG-STEPS-001,ADOG-JOBS-006");
        result.Should().Contain("Restrict the analysis");
        result.Should().Contain("DO, DO-NOT, AVOID, and CONSIDER");
        result.Should().Contain("Do not modify files");
    }

    [Fact]
    public void ReviewGuidelines_GivenNoFileOrPath_ShouldRequestAutomaticResolution()
    {
        string result = GuidelinePrompts.ReviewGuidelines("ADOG-STEPS-001,ADOG-JOBS-006");

        result.Should().Contain("Target: (automatic repository path resolution)");
        result.Should().Contain("Guideline IDs: ADOG-STEPS-001,ADOG-JOBS-006");
        result.Should().Contain("Restrict the analysis");
    }

    [Fact]
    public void ExplainGuideline_GivenGuidelineId_ShouldRequestGuidelineDetails()
    {
        string result = GuidelinePrompts.ExplainGuideline("ADOG-STEPS-001");

        result.Should().Contain("Guideline ID: ADOG-STEPS-001");
        result.Should().Contain("Call get_guideline");
        result.Should().Contain("recommendation label");
        result.Should().Contain("DO, DO-NOT, AVOID, and CONSIDER");
        result.Should().Contain("Do not propose file changes");
    }

    [Fact]
    public void FindGuidelines_GivenQueryAndCategory_ShouldRequestFilteredSearch()
    {
        string result = GuidelinePrompts.FindGuidelines("template", "steps");

        result.Should().Contain("Search text: template");
        result.Should().Contain("Category: steps");
        result.Should().Contain("Call search_guidelines");
        result.Should().Contain("recommendation labels");
        result.Should().Contain("DO, DO-NOT, AVOID, and CONSIDER");
    }

    [Fact]
    public void ListGuidelines_GivenNoCategory_ShouldRequestAllGuidelines()
    {
        string result = GuidelinePrompts.ListGuidelines();

        result.Should().Contain("Category: all");
        result.Should().Contain("Call list_guidelines");
        result.Should().Contain("recommendation label");
        result.Should().Contain("DO, DO-NOT, AVOID, and CONSIDER");
    }

    [Fact]
    public void RecommendationPrompts_ShouldUseOnlyRecommendationLabels()
    {
        foreach (string prompt in _recommendationPrompts)
        {
            prompt.Should().Contain("DO");
            prompt.Should().Contain("DO-NOT");
            prompt.Should().Contain("AVOID");
            prompt.Should().Contain("CONSIDER");
        }
    }

    [Fact]
    public void RecommendationPrompts_ShouldNotUseDiagnosticSeverityLabels()
    {
        foreach (string prompt in _recommendationPrompts)
        {
            prompt.Should().NotContain("Error");
            prompt.Should().NotContain("Warning");
            prompt.Should().NotContain("Info");
            prompt.Should().NotContain("severity");
            prompt.Should().NotContain("severities");
        }
    }

    [Fact]
    public void ListCategories_ShouldRequestAvailableCategories()
    {
        string result = GuidelinePrompts.ListCategories();

        result.Should().Contain("Call list_categories");
        result.Should().Contain("read-only");
        result.Should().NotContain("modify files");
    }
}
