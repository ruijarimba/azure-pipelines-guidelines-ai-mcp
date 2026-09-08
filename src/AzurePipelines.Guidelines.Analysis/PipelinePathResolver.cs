using System.Collections.Generic;

namespace AzurePipelines.Guidelines.Analysis;

/// <summary>
/// Expands one or more file or directory paths into the set of pipeline YAML files to analyse.
/// </summary>
public sealed class PipelinePathResolver
{
    private static readonly string[] _fallbackCandidates =
    [
        "pipelines",
        "azure-pipelines.yml",
        "azure-pipelines.yaml",
        ".azuredevops",
        ".ado",
        ".azure-pipelines"
    ];

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

    /// <summary>
    /// Resolves a requested path and, when it cannot be resolved, tries common pipeline
    /// locations in the current repository.
    /// </summary>
    /// <param name="inputPath">The requested file or directory path.</param>
    /// <param name="resolvedPaths">The pipeline files found by the request or a fallback candidate.</param>
    /// <param name="attemptedPaths">The request and every fallback candidate that was attempted.</param>
    /// <param name="error">A detailed error when the request and all candidates fail.</param>
    /// <param name="startingDirectory">The directory from which repository discovery starts.</param>
    /// <returns><see langword="true"/> when a path was resolved; otherwise <see langword="false"/>.</returns>
    public bool TryResolveWithRepositoryFallback(
        string inputPath,
        out IReadOnlyList<string> resolvedPaths,
        out IReadOnlyList<string> attemptedPaths,
        out string? error,
        string? startingDirectory = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);

        List<string> attempted = [inputPath];
        if (TryResolveSingle(inputPath, out resolvedPaths, out _))
        {
            attemptedPaths = attempted;
            error = null;
            return true;
        }

        string repositoryRoot = FindRepositoryRoot(startingDirectory ?? Environment.CurrentDirectory);
        foreach (string candidate in _fallbackCandidates)
        {
            string candidatePath = Path.Combine(repositoryRoot, candidate);
            if (attempted.Any(path => string.Equals(path, candidatePath, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            attempted.Add(candidatePath);
            if (TryResolveSingle(candidatePath, out resolvedPaths, out _))
            {
                attemptedPaths = attempted;
                error = null;
                return true;
            }
        }

        resolvedPaths = [];
        attemptedPaths = attempted;
        error = $"Path not found or does not contain pipeline YAML files: {inputPath}. " +
            $"Attempted: {string.Join(", ", attempted)}";
        return false;
    }

    private bool TryResolveSingle(
        string inputPath,
        out IReadOnlyList<string> resolvedPaths,
        out Exception? error)
    {
        try
        {
            resolvedPaths = Resolve([inputPath]);
            error = null;
            return true;
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or ArgumentException)
        {
            resolvedPaths = [];
            error = ex;
            return false;
        }
    }

    private static string FindRepositoryRoot(string startingDirectory)
    {
        DirectoryInfo? directory = new(Path.GetFullPath(startingDirectory));
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, ".git")) ||
                File.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Path.GetFullPath(startingDirectory);
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
