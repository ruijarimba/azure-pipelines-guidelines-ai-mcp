using System.CommandLine;
using System.CommandLine.Parsing;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Cli.Tests;

public sealed class AnalyzeCommandEnvironmentTests
{
    [Theory]
    [InlineData("true", true)]
    [InlineData("1", true)]
    [InlineData("yes", true)]
    [InlineData("false", false)]
    [InlineData("0", false)]
    [InlineData("no", false)]
    public void Load_GivenSupportedBooleanEnvironmentValues_ShouldParseThem(string value, bool expected)
    {
        using EnvironmentVariableScope scope = new("ADOG_SOFT_FAIL", value);

        AnalyzeCommandEnvironment environment = AnalyzeCommandEnvironment.Load();

        environment.ErrorMessage.Should().BeNull();
        environment.SoftFail.Should().Be(expected);
    }

    [Theory]
    [InlineData("ADOG_NO_COLOR")]
    [InlineData("ADOG_QUIET")]
    [InlineData("ADOG_VERBOSE")]
    public void Load_GivenInvalidBooleanEnvironmentValue_ShouldReturnTheFirstRelevantError(string variableName)
    {
        using EnvironmentVariableScope scope = new(variableName, "maybe");

        AnalyzeCommandEnvironment environment = AnalyzeCommandEnvironment.Load();

        environment.ErrorMessage.Should().Contain(variableName);
    }

    [Fact]
    public void Load_GivenWhitespaceBooleanEnvironmentValue_ShouldTreatItAsUnset()
    {
        using EnvironmentVariableScope scope = new("ADOG_SOFT_FAIL", "  ");

        AnalyzeCommandEnvironment.Load().SoftFail.Should().BeNull();
    }

    [Fact]
    public void IsSetByUser_GivenMatchingOptionToken_ShouldReturnTrue()
    {
        Option<string> format = new("--format");
        ParseResult parseResult = new RootCommand { format }.Parse("--format json");

        AnalyzeCommandEnvironment.IsSetByUser(parseResult, "--format").Should().BeTrue();
        AnalyzeCommandEnvironment.IsSetByUser(parseResult, "--other").Should().BeFalse();
    }

    private sealed class EnvironmentVariableScope : IDisposable
    {
        private readonly string _name;
        private readonly string? _originalValue;

        internal EnvironmentVariableScope(string name, string value)
        {
            _name = name;
            _originalValue = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, value);
        }

        public void Dispose() => Environment.SetEnvironmentVariable(_name, _originalValue);
    }
}
