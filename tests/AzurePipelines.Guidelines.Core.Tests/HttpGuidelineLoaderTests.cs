using System.Net;
using System.Text;
using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Core.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Core.Tests;

public sealed class HttpGuidelineLoaderTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    // Ownership of the handler is intentionally transferred to HttpClient.
    // The caller is responsible for disposing the returned HttpClient (use 'using').
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "HttpClient takes ownership of the handler via disposeHandler:true.")]
    private static HttpClient MakeClient(string responseJson) =>
        new(new FakeHttpMessageHandler(responseJson), disposeHandler: true);

    // ── Constructor ───────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_GivenNullHttpClient_ShouldThrowArgumentNullException()
    {
        // Arrange / Act
        Action act = () => _ = new HttpGuidelineLoader(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    // ── DefaultManifestUrl ────────────────────────────────────────────────────

    [Fact]
    public void DefaultManifestUrl_ShouldPointToCompanionRepo()
    {
        // Arrange / Act
        Uri url = HttpGuidelineLoader.DefaultManifestUrl;

        // Assert
        url.Host.Should().Be("raw.githubusercontent.com");
        url.AbsolutePath.Should().Contain("azure-pipelines-guidelines");
        url.AbsolutePath.Should().EndWith("guidelines.json");
    }

    // ── LoadAsync: empty / malformed responses ────────────────────────────────

    [Fact]
    public async Task LoadAsync_GivenEmptyGuidelinesArray_ShouldReturnEmptyList()
    {
        // Arrange
        string json = TestFixtures.Load("HttpGuidelineLoader/EmptyGuidelines.json");
        using HttpClient client = MakeClient(json);
        HttpGuidelineLoader sut = new(client);

        // Act
        IReadOnlyList<GuidelineDefinition> result = await sut.LoadAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_GivenNullGuidelinesProperty_ShouldReturnEmptyList()
    {
        // Arrange
        using HttpClient client = MakeClient("{ \"guidelines\": null }");
        HttpGuidelineLoader sut = new(client);

        // Act
        IReadOnlyList<GuidelineDefinition> result = await sut.LoadAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_GivenMissingGuidelinesProperty_ShouldReturnEmptyList()
    {
        // Arrange
        string json = TestFixtures.Load("HttpGuidelineLoader/MissingGuidelinesProperty.json");
        using HttpClient client = MakeClient(json);
        HttpGuidelineLoader sut = new(client);

        // Act
        IReadOnlyList<GuidelineDefinition> result = await sut.LoadAsync();

        // Assert
        result.Should().BeEmpty();
    }

    // ── LoadAsync: valid single guideline ─────────────────────────────────────

    [Fact]
    public async Task LoadAsync_GivenValidGuideline_ShouldReturnOneDefinition()
    {
        // Arrange
        string json = TestFixtures.Load("HttpGuidelineLoader/ValidGuideline.json");
        using HttpClient client = MakeClient(json);
        HttpGuidelineLoader sut = new(client);

        // Act
        IReadOnlyList<GuidelineDefinition> result = await sut.LoadAsync();

        // Assert
        result.Should().HaveCount(1);
        GuidelineDefinition g = result[0];
        g.Id.Value.Should().Be("ADOG-STEPS-001");
        g.Category.Should().Be(GuidelineCategory.Steps);
        g.Severity.Should().Be(GuidelineSeverity.Do);
        g.Title.Should().Be("DO: Use script tasks");
        g.Description.Should().Be("Use script tasks for shell commands.");
    }

    // ── LoadAsync: severity mapping ───────────────────────────────────────────

    [Theory]
    [InlineData("do", GuidelineSeverity.Do)]
    [InlineData("do-not", GuidelineSeverity.DoNot)]
    [InlineData("avoid", GuidelineSeverity.Avoid)]
    [InlineData("consider", GuidelineSeverity.Consider)]
    public async Task LoadAsync_GivenKnownSeverity_ShouldMapCorrectly(
        string jsonSeverity, GuidelineSeverity expected)
    {
        // Arrange
        using HttpClient client = MakeClient($$"""
            {
              "guidelines": [
                {
                  "id": "ADOG-STEPS-001",
                  "category": "steps",
                  "severity": "{{jsonSeverity}}",
                  "title": "T",
                  "summary": "S"
                }
              ]
            }
            """);
        HttpGuidelineLoader sut = new(client);

        // Act
        IReadOnlyList<GuidelineDefinition> result = await sut.LoadAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Severity.Should().Be(expected);
    }

    // ── LoadAsync: invalid / skipped items ────────────────────────────────────

    [Fact]
    public async Task LoadAsync_GivenUnknownCategory_ShouldSkipThatGuideline()
    {
        // Arrange
        using HttpClient client = MakeClient("""
            {
              "guidelines": [
                {
                  "id": "ADOG-STEPS-001",
                  "category": "unknown-category",
                  "severity": "do",
                  "title": "T",
                  "summary": "S"
                }
              ]
            }
            """);
        HttpGuidelineLoader sut = new(client);

        // Act
        IReadOnlyList<GuidelineDefinition> result = await sut.LoadAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_GivenMissingId_ShouldSkipThatGuideline()
    {
        // Arrange
        using HttpClient client = MakeClient("""
            {
              "guidelines": [
                {
                  "category": "steps",
                  "severity": "do",
                  "title": "T",
                  "summary": "S"
                }
              ]
            }
            """);
        HttpGuidelineLoader sut = new(client);

        // Act
        IReadOnlyList<GuidelineDefinition> result = await sut.LoadAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_GivenInvalidIdFormat_ShouldSkipThatGuideline()
    {
        // Arrange
        using HttpClient client = MakeClient("""
            {
              "guidelines": [
                {
                  "id": "INVALID-ID",
                  "category": "steps",
                  "severity": "do",
                  "title": "T",
                  "summary": "S"
                }
              ]
            }
            """);
        HttpGuidelineLoader sut = new(client);

        // Act
        IReadOnlyList<GuidelineDefinition> result = await sut.LoadAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_GivenMixOfValidAndInvalid_ShouldReturnOnlyValidGuidelines()
    {
        // Arrange
        using HttpClient client = MakeClient("""
            {
              "guidelines": [
                {
                  "id": "ADOG-STEPS-001",
                  "category": "steps",
                  "severity": "do",
                  "title": "Valid",
                  "summary": "S"
                },
                {
                  "id": "INVALID-ID",
                  "category": "steps",
                  "severity": "do",
                  "title": "Invalid",
                  "summary": "S"
                }
              ]
            }
            """);
        HttpGuidelineLoader sut = new(client);

        // Act
        IReadOnlyList<GuidelineDefinition> result = await sut.LoadAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Value.Should().Be("ADOG-STEPS-001");
    }

    // ── LoadAsync: detection hints ────────────────────────────────────────────

    [Fact]
    public async Task LoadAsync_GivenDetectionHint_ShouldMapKindAndScope()
    {
        // Arrange
        using HttpClient client = MakeClient("""
            {
              "guidelines": [
                {
                  "id": "ADOG-STEPS-001",
                  "category": "steps",
                  "severity": "do",
                  "title": "T",
                  "summary": "S",
                  "detection": [
                    {
                      "kind": "heuristic",
                      "pattern": "some pattern",
                      "appliesTo": ["steps"],
                      "message": "A message."
                    }
                  ]
                }
              ]
            }
            """);
        HttpGuidelineLoader sut = new(client);

        // Act
        IReadOnlyList<GuidelineDefinition> result = await sut.LoadAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].DetectionHints.Should().HaveCount(1);
        DetectionHint hint = result[0].DetectionHints[0];
        hint.Kind.Should().Be(DetectionKind.Heuristic);
        hint.Scope.Should().Be(PipelineScope.Step);
        hint.Description.Should().Be("A message.");
    }

    [Fact]
    public async Task LoadAsync_GivenGuidelineMetadata_ShouldReadUrlAndAppliesToProperties()
    {
        // Arrange
        using HttpClient client = MakeClient("""
            {
              "guidelines": [
                {
                  "id": "ADOG-STEPS-001",
                  "category": "steps",
                  "severity": "do",
                  "title": "T",
                  "summary": "S",
                  "url": "https://example.test/guideline",
                  "appliesTo": ["steps"]
                }
              ]
            }
            """);
        HttpGuidelineLoader sut = new(client);

        // Act
        IReadOnlyList<GuidelineDefinition> result = await sut.LoadAsync();

        // Assert
        result.Should().ContainSingle();
        result[0].References.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_GivenDetectionHintWithNullKind_ShouldSkipThatHint()
    {
        // Arrange
        using HttpClient client = MakeClient("""
            {
              "guidelines": [
                {
                  "id": "ADOG-STEPS-001",
                  "category": "steps",
                  "severity": "do",
                  "title": "T",
                  "summary": "S",
                  "detection": [
                    {
                      "pattern": "some pattern",
                      "message": "A message."
                    }
                  ]
                }
              ]
            }
            """);
        HttpGuidelineLoader sut = new(client);

        // Act
        IReadOnlyList<GuidelineDefinition> result = await sut.LoadAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].DetectionHints.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_GivenDetectionHintWithUnknownKind_ShouldSkipThatHint()
    {
        // Arrange
        using HttpClient client = MakeClient("""
            {
              "guidelines": [
                {
                  "id": "ADOG-STEPS-001",
                  "category": "steps",
                  "severity": "do",
                  "title": "T",
                  "summary": "S",
                  "detection": [
                    {
                      "kind": "not-a-valid-kind",
                      "pattern": "some pattern",
                      "message": "A message."
                    }
                  ]
                }
              ]
            }
            """);
        HttpGuidelineLoader sut = new(client);

        // Act
        IReadOnlyList<GuidelineDefinition> result = await sut.LoadAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].DetectionHints.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_GivenDetectionHintWithNoAppliesTo_ShouldDefaultScopeToGeneral()
    {
        // Arrange
        using HttpClient client = MakeClient("""
            {
              "guidelines": [
                {
                  "id": "ADOG-STEPS-001",
                  "category": "steps",
                  "severity": "do",
                  "title": "T",
                  "summary": "S",
                  "detection": [
                    {
                      "kind": "regex",
                      "pattern": "some pattern",
                      "message": "A message."
                    }
                  ]
                }
              ]
            }
            """);
        HttpGuidelineLoader sut = new(client);

        // Act
        IReadOnlyList<GuidelineDefinition> result = await sut.LoadAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].DetectionHints.Should().HaveCount(1);
        result[0].DetectionHints[0].Scope.Should().Be(PipelineScope.General);
    }

    [Fact]
    public async Task LoadAsync_GivenFixWithNullSummary_ShouldNotMapFix()
    {
        // Arrange
        using HttpClient client = MakeClient("""
            {
              "guidelines": [
                {
                  "id": "ADOG-STEPS-001",
                  "category": "steps",
                  "severity": "do",
                  "title": "T",
                  "summary": "S",
                  "fix": {}
                }
              ]
            }
            """);
        HttpGuidelineLoader sut = new(client);

        // Act
        IReadOnlyList<GuidelineDefinition> result = await sut.LoadAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Fix.Should().BeNull();
    }

    // ── LoadAsync: fix guidance ───────────────────────────────────────────────

    [Fact]
    public async Task LoadAsync_GivenFixWithSummary_ShouldMapFixGuidance()
    {
        // Arrange
        using HttpClient client = MakeClient("""
            {
              "guidelines": [
                {
                  "id": "ADOG-STEPS-001",
                  "category": "steps",
                  "severity": "do",
                  "title": "T",
                  "summary": "S",
                  "fix": { "summary": "Do this instead." }
                }
              ]
            }
            """);
        HttpGuidelineLoader sut = new(client);

        // Act
        IReadOnlyList<GuidelineDefinition> result = await sut.LoadAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Fix.Should().NotBeNull();
        result[0].Fix!.Summary.Should().Be("Do this instead.");
    }

    // ── LoadAsync: category mapping ───────────────────────────────────────────

    [Theory]
    [InlineData("general", GuidelineCategory.General, "ADOG-GENERAL-001")]
    [InlineData("jobs", GuidelineCategory.Jobs, "ADOG-JOBS-001")]
    [InlineData("parameters", GuidelineCategory.Parameters, "ADOG-PARAMETERS-001")]
    [InlineData("pipelines", GuidelineCategory.Pipelines, "ADOG-PIPELINES-001")]
    [InlineData("stages", GuidelineCategory.Stages, "ADOG-STAGES-001")]
    [InlineData("variables", GuidelineCategory.Variables, "ADOG-VARIABLES-001")]
    public async Task LoadAsync_GivenKnownCategory_ShouldMapCorrectly(
        string jsonCategory, GuidelineCategory expected, string id)
    {
        // Arrange
        using HttpClient client = MakeClient($$"""
            {
              "guidelines": [
                {
                  "id": "{{id}}",
                  "category": "{{jsonCategory}}",
                  "severity": "do",
                  "title": "T",
                  "summary": "S"
                }
              ]
            }
            """);
        HttpGuidelineLoader sut = new(client);

        // Act
        IReadOnlyList<GuidelineDefinition> result = await sut.LoadAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Category.Should().Be(expected);
    }

    [Fact]
    public async Task LoadAsync_GivenUnknownSeverity_ShouldSkipThatGuideline()
    {
        // Arrange
        using HttpClient client = MakeClient("""
            {
              "guidelines": [
                {
                  "id": "ADOG-STEPS-001",
                  "category": "steps",
                  "severity": "unknown-severity",
                  "title": "T",
                  "summary": "S"
                }
              ]
            }
            """);
        HttpGuidelineLoader sut = new(client);

        // Act
        IReadOnlyList<GuidelineDefinition> result = await sut.LoadAsync();

        // Assert
        result.Should().BeEmpty();
    }

    // ── LoadAsync: detection hint kind variants ───────────────────────────────

    [Theory]
    [InlineData("yaml-path", DetectionKind.YamlPath)]
    [InlineData("yamlpath", DetectionKind.YamlPath)]
    [InlineData("regex", DetectionKind.Regex)]
    public async Task LoadAsync_GivenDetectionKindVariant_ShouldMapCorrectly(
        string jsonKind, DetectionKind expected)
    {
        // Arrange
        using HttpClient client = MakeClient($$"""
            {
              "guidelines": [
                {
                  "id": "ADOG-STEPS-001",
                  "category": "steps",
                  "severity": "do",
                  "title": "T",
                  "summary": "S",
                  "detection": [
                    {
                      "kind": "{{jsonKind}}",
                      "message": "Msg"
                    }
                  ]
                }
              ]
            }
            """);
        HttpGuidelineLoader sut = new(client);

        // Act
        IReadOnlyList<GuidelineDefinition> result = await sut.LoadAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].DetectionHints.Should().HaveCount(1);
        result[0].DetectionHints[0].Kind.Should().Be(expected);
    }

    // ── LoadAsync: detection hint scope variants ──────────────────────────────

    [Theory]
    [InlineData("pipeline", PipelineScope.Pipeline)]
    [InlineData("stage", PipelineScope.Stage)]
    [InlineData("stages", PipelineScope.Stage)]
    [InlineData("job", PipelineScope.Job)]
    [InlineData("jobs", PipelineScope.Job)]
    [InlineData("task", PipelineScope.Task)]
    [InlineData("variable", PipelineScope.Variables)]
    [InlineData("variables", PipelineScope.Variables)]
    [InlineData("parameter", PipelineScope.Parameters)]
    [InlineData("parameters", PipelineScope.Parameters)]
    [InlineData("template", PipelineScope.Template)]
    [InlineData("general", PipelineScope.General)]
    [InlineData("unknown-scope", PipelineScope.General)]
    public async Task LoadAsync_GivenDetectionScope_ShouldMapCorrectly(
        string jsonScope, PipelineScope expected)
    {
        // Arrange
        using HttpClient client = MakeClient($$"""
            {
              "guidelines": [
                {
                  "id": "ADOG-STEPS-001",
                  "category": "steps",
                  "severity": "do",
                  "title": "T",
                  "summary": "S",
                  "detection": [
                    {
                      "kind": "regex",
                      "appliesTo": ["{{jsonScope}}"],
                      "message": "Msg"
                    }
                  ]
                }
              ]
            }
            """);
        HttpGuidelineLoader sut = new(client);

        // Act
        IReadOnlyList<GuidelineDefinition> result = await sut.LoadAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].DetectionHints.Should().HaveCount(1);
        result[0].DetectionHints[0].Scope.Should().Be(expected);
    }

    // ── FakeHttpMessageHandler ────────────────────────────────────────────────

    private sealed class FakeHttpMessageHandler(
        string responseBody,
        HttpStatusCode statusCode = HttpStatusCode.OK) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage response = new(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
            };

            return Task.FromResult(response);
        }
    }
}
