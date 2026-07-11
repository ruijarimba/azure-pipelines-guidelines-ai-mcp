using AzurePipelines.Guidelines.Analysis;
using FluentAssertions;
using Xunit;

namespace AzurePipelines.Guidelines.Analysis.Tests;

public sealed class PipelinePathResolverTests
{
    [Fact]
    public void Resolve_GivenNullInputPaths_ShouldThrowArgumentNullException()
    {
        PipelinePathResolver sut = new();

        Action act = () => sut.Resolve(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Resolve_GivenEmptyPath_ShouldThrowArgumentException()
    {
        PipelinePathResolver sut = new();

        Action act = () => sut.Resolve([""]);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Resolve_GivenSingleYamlFile_ShouldReturnThatFile()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string filePath = Path.Combine(tempDirectory, "pipeline.yml");
            File.WriteAllText(filePath, "trigger: none");
            PipelinePathResolver sut = new();

            IReadOnlyList<string> result = sut.Resolve([filePath]);

            result.Should().Equal(Path.GetFullPath(filePath));
        }
        finally
        {
            DeleteTempDirectory(tempDirectory);
        }
    }

    [Fact]
    public void Resolve_GivenDirectoryWithMixedFiles_ShouldReturnOnlyYamlFilesInSortedOrder()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string nestedDirectory = Path.Combine(tempDirectory, "nested");
            Directory.CreateDirectory(nestedDirectory);

            string firstFile = Path.Combine(tempDirectory, "z.yaml");
            string secondFile = Path.Combine(nestedDirectory, "a.yml");
            string ignoredFile = Path.Combine(nestedDirectory, "notes.txt");

            File.WriteAllText(firstFile, "trigger: none");
            File.WriteAllText(secondFile, "jobs:");
            File.WriteAllText(ignoredFile, "ignore me");

            PipelinePathResolver sut = new();

            IReadOnlyList<string> result = sut.Resolve([tempDirectory]);

            result.Should().BeEquivalentTo(
                [Path.GetFullPath(secondFile), Path.GetFullPath(firstFile)],
                options => options.WithStrictOrdering());
        }
        finally
        {
            DeleteTempDirectory(tempDirectory);
        }
    }

    [Fact]
    public void Resolve_GivenNonYamlFile_ShouldThrowArgumentException()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string filePath = Path.Combine(tempDirectory, "README.md");
            File.WriteAllText(filePath, "readme");
            PipelinePathResolver sut = new();

            Action act = () => sut.Resolve([filePath]);

            act.Should().Throw<ArgumentException>()
                .WithMessage("*pipeline YAML file*");
        }
        finally
        {
            DeleteTempDirectory(tempDirectory);
        }
    }

    [Fact]
    public void Resolve_GivenMissingPath_ShouldThrowFileNotFoundException()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string missingPath = Path.Combine(tempDirectory, "missing");
            PipelinePathResolver sut = new();

            Action act = () => sut.Resolve([missingPath]);

            act.Should().Throw<FileNotFoundException>();
        }
        finally
        {
            DeleteTempDirectory(tempDirectory);
        }
    }

    [Fact]
    public void Resolve_GivenDirectoryWithoutYamlFiles_ShouldThrowInvalidOperationException()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            File.WriteAllText(Path.Combine(tempDirectory, "notes.txt"), "ignore me");
            PipelinePathResolver sut = new();

            Action act = () => sut.Resolve([tempDirectory]);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*No pipeline YAML files found*");
        }
        finally
        {
            DeleteTempDirectory(tempDirectory);
        }
    }

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), $"pipeline-path-resolver-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteTempDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
