using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Cli.Tests;

public sealed class CliConfigurationLoaderTests
{
    [Fact]
    public void Load_GivenNoConfigurationFile_ShouldReturnEmptyConfiguration()
    {
        string directory = CreateDirectory();
        try
        {
            using CurrentDirectoryScope scope = new(directory);

            CliConfiguration configuration = CliConfigurationLoader.Load();

            configuration.GetFormatValue().Should().BeNull();
            configuration.ErrorMessage.Should().BeNull();
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public void Load_GivenConfigurationFile_ShouldReadAllValues()
    {
        string directory = CreateDirectory();
        try
        {
            File.WriteAllText(Path.Combine(directory, "adog.json"),
                "{\"format\":\"json\",\"severity\":\"warning\",\"category\":\"steps\",\"output\":\"out.json\",\"soft-fail\":\"yes\",\"no-color\":true,\"quiet\":0,\"verbose\":\"false\"}");
            using CurrentDirectoryScope scope = new(directory);

            CliConfiguration configuration = CliConfigurationLoader.Load();

            configuration.GetFormatValue().Should().Be("json");
            configuration.GetSeverityValue().Should().Be("warning");
            configuration.GetCategoryValue().Should().Be("steps");
            configuration.GetOutputValue().Should().Be("out.json");
            configuration.GetSoftFailValue().Should().BeTrue();
            configuration.GetNoColorValue().Should().BeTrue();
            configuration.GetQuietValue().Should().BeFalse();
            configuration.GetVerboseValue().Should().BeFalse();
            configuration.ErrorMessage.Should().BeNull();
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Theory]
    [InlineData("soft-fail")]
    [InlineData("no-color")]
    [InlineData("quiet")]
    [InlineData("verbose")]
    public void Load_GivenInvalidBoolean_ShouldReturnValidationError(string property)
    {
        string directory = CreateDirectory();
        try
        {
            File.WriteAllText(Path.Combine(directory, "adog.json"), $"{{\"{property}\":\"maybe\"}}");
            using CurrentDirectoryScope scope = new(directory);

            CliConfiguration configuration = CliConfigurationLoader.Load();

            configuration.ErrorMessage.Should().Contain($"config property '{property}'");
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public void Load_GivenInvalidJson_ShouldReturnJsonError()
    {
        string directory = CreateDirectory();
        try
        {
            File.WriteAllText(Path.Combine(directory, "adog.json"), "{ invalid");
            using CurrentDirectoryScope scope = new(directory);

            CliConfiguration configuration = CliConfigurationLoader.Load();

            configuration.ErrorMessage.Should().StartWith("error: Invalid JSON in config file");
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public void Load_GivenJsonArray_ShouldReturnEmptyConfiguration()
    {
        string directory = CreateDirectory();
        try
        {
            File.WriteAllText(Path.Combine(directory, "adog.json"), "[]");
            using CurrentDirectoryScope scope = new(directory);

            CliConfiguration configuration = CliConfigurationLoader.Load();

            configuration.GetFormatValue().Should().BeNull();
            configuration.ErrorMessage.Should().BeNull();
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("1", true)]
    [InlineData("yes", true)]
    [InlineData("false", false)]
    [InlineData("0", false)]
    [InlineData("no", false)]
    public void TryValidate_GivenSupportedBooleanValues_ShouldReturnParsedValue(string value, bool expected)
    {
        CliConfiguration configuration = new() { SoftFail = value };

        bool valid = configuration.TryValidate(out string? errorMessage);

        valid.Should().BeTrue();
        errorMessage.Should().BeNull();
        configuration.GetSoftFailValue().Should().Be(expected);
    }

    [Fact]
    public void TryValidate_GivenWhitespaceBoolean_ShouldTreatItAsUnset()
    {
        CliConfiguration configuration = new() { SoftFail = "  " };

        configuration.TryValidate(out string? errorMessage).Should().BeTrue();
        errorMessage.Should().BeNull();
        configuration.GetSoftFailValue().Should().BeNull();
    }

    private static string CreateDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"adog-config-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void DeleteDirectory(string directory)
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class CurrentDirectoryScope : IDisposable
    {
        private readonly string _originalDirectory = Environment.CurrentDirectory;

        internal CurrentDirectoryScope(string directory) => Directory.SetCurrentDirectory(directory);

        public void Dispose() => Directory.SetCurrentDirectory(_originalDirectory);
    }
}
