using System.Collections.Generic;

namespace AzurePipelines.Guidelines.Analysis;

/// <summary>
/// Expands one or more file or directory paths into the set of pipeline YAML files to analyse.
/// </summary>
public sealed class PipelinePathResolver
{
    /// <summary>
    /// Resolves a collection of file and directory paths into a deterministic list of pipeline YAML files.
    /// </summary>
    /// <param name="inputPaths">One or more file or directory paths.</param>
    /// <returns>A sorted list of discovered pipeline YAML files.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="inputPaths"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when an input path is empty or points to a non-YAML file.</exception>
    /// <exception cref="FileNotFoundException">Thrown when an input path does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a directory contains no pipeline YAML files.</exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1822:MarkMembersAsStatic",
        Justification = "Used via dependency injection for consistency with other service-like helpers.")]
    public IReadOnlyList<string> Resolve(IEnumerable<string> inputPaths)
    {
        ArgumentNullException.ThrowIfNull(inputPaths);

        List<string> discoveredFiles = [];

        foreach (string inputPath in inputPaths)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);

            string fullPath = Path.GetFullPath(inputPath);

            if (File.Exists(fullPath))
            {
                ValidatePipelineFile(fullPath);
                discoveredFiles.Add(fullPath);
                continue;
            }

            if (Directory.Exists(fullPath))
            {
                List<string> directoryFiles = Directory
                    .EnumerateFiles(fullPath, "*", SearchOption.AllDirectories)
                    .Where(IsPipelineYamlFile)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (directoryFiles.Count == 0)
                {
                    throw new InvalidOperationException($"No pipeline YAML files found in directory: {inputPath}");
                }

                discoveredFiles.AddRange(directoryFiles);
                continue;
            }

            throw new FileNotFoundException($"Path not found: {inputPath}", fullPath);
        }

        return [.. discoveredFiles
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)];
    }

    private static void ValidatePipelineFile(string fullPath)
    {
        if (!IsPipelineYamlFile(fullPath))
        {
            throw new ArgumentException($"Path is not a pipeline YAML file: {fullPath}", nameof(fullPath));
        }
    }

    private static bool IsPipelineYamlFile(string fullPath) =>
        fullPath.EndsWith(".yml", StringComparison.OrdinalIgnoreCase) ||
        fullPath.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase);
}
