using AzurePipelines.Guidelines.Core;
using AzurePipelines.Guidelines.Mcp;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace AzurePipelines.Guidelines.Mcp.Tests;

public sealed class McpToolInvocationLogTests
{
    [Fact]
    public void Log_GivenAnalysisOptions_ShouldIncludeOperationalSettings()
    {
        // Arrange
        TestLogger logger = new();
        AnalysisOptions options = new(
            MinimumSeverity: DiagnosticSeverity.Warning,
            IncludedCategories: [GuidelineCategory.Steps],
            IncludedGuidelineIds: [new GuidelineId("ADOG-STEPS-001")],
            IncludedDiagnosticSeverities: [DiagnosticSeverity.Warning]);

        // Act
        McpToolInvocationLog.Log(logger, "analyze_pipeline", "steps", "ADOG-STEPS-001", options);

        // Assert
        logger.Message.Should().Contain("analyze_pipeline");
        logger.Message.Should().Contain("category=steps");
        logger.Message.Should().Contain("guidelineIds=ADOG-STEPS-001");
        logger.Message.Should().Contain("minimumSeverity=Warning");
        logger.Message.Should().Contain("includedCategories=Steps");
        logger.Message.Should().Contain("includedGuidelineIds=ADOG-STEPS-001");
        logger.Message.Should().Contain("includedDiagnosticSeverities=Warning");
    }

    [Fact]
    public void Log_GivenPayloadLikeValuesAreNotOptions_ShouldNotIncludeThem()
    {
        // Arrange
        TestLogger logger = new();

        // Act
        McpToolInvocationLog.Log(logger, "analyze_pipeline", category: "steps");

        // Assert
        logger.Message.Should().Contain("category=steps");
        logger.Message.Should().NotContain("steps:");
        logger.Message.Should().NotContain("template-name");
        logger.Message.Should().NotContain("secret-value");
    }

    [Fact]
    public void Log_GivenDefaultOptions_ShouldShowAllValuesAsDefaults()
    {
        // Arrange
        TestLogger logger = new();

        // Act
        McpToolInvocationLog.Log(logger, "list_guidelines");

        // Assert
        logger.Message.Should().Contain("category=<default>");
        logger.Message.Should().Contain("guidelineIds=<default>");
    }

    private sealed class TestLogger : ILogger
    {
        public string Message { get; private set; } = string.Empty;

        public IDisposable BeginScope<TState>(TState state) where TState : notnull =>
            NoopDisposable.Instance;

        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Information;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Message = formatter(state, exception);

        private sealed class NoopDisposable : IDisposable
        {
            internal static NoopDisposable Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }
}
