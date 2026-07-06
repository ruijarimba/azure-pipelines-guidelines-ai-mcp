using System.Reflection;

namespace AzurePipelines.Guidelines.Parsing.Tests.Fixtures;

/// <summary>
/// Helper class for loading test fixture files.
/// </summary>
internal static class TestFixtures
{
    /// <summary>
    /// Loads a fixture file from the Fixtures folder.
    /// </summary>
    /// <param name="relativePath">
    /// Path relative to the Fixtures folder, for example "YamlPipelineParser/EmptyPipeline.yml".
    /// </param>
    /// <returns>The file content as a string.</returns>
    /// <exception cref="FileNotFoundException">
    /// Thrown when the fixture file does not exist.
    /// </exception>
    public static string Load(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        string assemblyLocation = Assembly.GetExecutingAssembly().Location;
        string assemblyDirectory = Path.GetDirectoryName(assemblyLocation)
            ?? throw new InvalidOperationException("Could not determine assembly directory.");

        string fixturePath = Path.Combine(
            assemblyDirectory,
            "Fixtures",
            relativePath);

        if (!File.Exists(fixturePath))
        {
            throw new FileNotFoundException(
                $"Fixture file not found: {relativePath}",
                fixturePath);
        }

        return File.ReadAllText(fixturePath);
    }
}
