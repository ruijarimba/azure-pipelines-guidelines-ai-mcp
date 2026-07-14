using AzurePipelines.Guidelines.Core;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace AzurePipelines.Guidelines.Parsing;

/// <summary>
/// Parses Azure Pipelines YAML text into a <see cref="PipelineDocument"/> using YamlDotNet.
/// All YamlDotNet types are confined to this class.
/// </summary>
internal sealed class YamlPipelineParser : IPipelineParser
{
    /// <inheritdoc/>
    public PipelineDocument Parse(string yaml, string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(yaml);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        YamlMappingNode root = LoadRootMapping(yaml, filePath);

        IReadOnlyList<ParameterNode> parameters = ParseParameters(root);
        IReadOnlyList<VariableNode> variables = ParseVariables(root);
        IReadOnlyList<StageNode> stages = ParseStages(root);
        IReadOnlyList<JobNode> jobs = ParseJobs(root);
        IReadOnlyList<StepNode> steps = ParseSteps(root);

        return new PipelineDocument(filePath, yaml, parameters, variables, stages, jobs, steps);
    }

    /// <summary>Loads and validates the YAML document root mapping.</summary>
    /// <param name="yaml">The YAML text to parse.</param>
    /// <param name="filePath">The source path used in parsing errors.</param>
    /// <returns>The root YAML mapping.</returns>
    /// <exception cref="PipelineParsingException">The YAML is invalid or has no mapping root.</exception>
    private static YamlMappingNode LoadRootMapping(string yaml, string filePath)
    {
        using StringReader reader = new(yaml);
        YamlStream stream = new();

        try
        {
            stream.Load(reader);
        }
        catch (YamlException ex)
        {
            throw new PipelineParsingException(
                $"Failed to parse YAML in '{filePath}': {ex.Message}", ex);
        }

        if (stream.Documents.Count == 0)
        {
            throw new PipelineParsingException(
                $"The file '{filePath}' contains no YAML documents.");
        }

        if (stream.Documents[0].RootNode is not YamlMappingNode mapping)
        {
            throw new PipelineParsingException(
                $"The root of '{filePath}' is not a YAML mapping node.");
        }

        return mapping;
    }

    /// <summary>Parses top-level parameter declarations.</summary>
    /// <param name="root">The YAML document root.</param>
    /// <returns>Valid parameter nodes in source order.</returns>
    private static List<ParameterNode> ParseParameters(YamlMappingNode root)
    {
        if (!TryGetSequence(root, "parameters", out YamlSequenceNode? seq))
        {
            return [];
        }

        List<ParameterNode> result = [];

        foreach (YamlNode item in seq!.Children)
        {
            if (item is not YamlMappingNode map)
            {
                continue;
            }

            string? name = ScalarOrNull(map, "name");
            if (name is null)
            {
                continue;
            }

            string? type = ScalarOrNull(map, "type");
            string? defaultVal = ScalarOrNull(map, "default");
            IReadOnlyList<string> values = ParseScalarList(map, "values");

            result.Add(new ParameterNode(name, type, defaultVal, values));
        }

        return result;
    }

    /// <summary>Parses variables from either sequence or mapping syntax.</summary>
    /// <param name="root">The YAML mapping containing the variables entry.</param>
    /// <returns>Variable nodes in source order.</returns>
    private static List<VariableNode> ParseVariables(YamlMappingNode root)
    {
        if (!TryGetNode(root, "variables", out YamlNode? node))
        {
            return [];
        }

        return node switch
        {
            YamlSequenceNode seq => ParseVariableSequence(seq),
            YamlMappingNode map => ParseVariableMapping(map),
            _ => []
        };
    }

    /// <summary>Parses variables declared as a YAML sequence.</summary>
    /// <param name="seq">The variable sequence.</param>
    /// <returns>Variable nodes in source order.</returns>
    private static List<VariableNode> ParseVariableSequence(YamlSequenceNode seq)
    {
        List<VariableNode> result = [];

        foreach (YamlNode item in seq.Children)
        {
            if (item is not YamlMappingNode map)
            {
                continue;
            }

            string? group = ScalarOrNull(map, "group");
            if (group is not null)
            {
                result.Add(new VariableNode(Name: null, Value: null, IsReadOnly: false, Group: group));
                continue;
            }

            string? name = ScalarOrNull(map, "name");
            string? value = ScalarOrNull(map, "value");
            bool isReadOnly = BoolOrFalse(map, "readonly");
            result.Add(new VariableNode(name, value, isReadOnly, Group: null));
        }

        return result;
    }

    /// <summary>Parses variables declared as a YAML mapping.</summary>
    /// <param name="map">The variable mapping.</param>
    /// <returns>Variable nodes in mapping order.</returns>
    private static List<VariableNode> ParseVariableMapping(YamlMappingNode map)
    {
        List<VariableNode> result = [];

        foreach (KeyValuePair<YamlNode, YamlNode> entry in map.Children)
        {
            string? key = (entry.Key as YamlScalarNode)?.Value;
            string? val = (entry.Value as YamlScalarNode)?.Value;
            if (key is not null)
            {
                result.Add(new VariableNode(key, val, IsReadOnly: false, Group: null));
            }
        }

        return result;
    }

    /// <summary>Parses top-level stage declarations.</summary>
    /// <param name="root">The YAML document root.</param>
    /// <returns>Stage nodes in source order.</returns>
    private static List<StageNode> ParseStages(YamlMappingNode root)
    {
        if (!TryGetSequence(root, "stages", out YamlSequenceNode? seq))
        {
            return [];
        }

        List<StageNode> result = [];

        foreach (YamlNode item in seq!.Children)
        {
            if (item is not YamlMappingNode map)
            {
                continue;
            }

            int? line = LineOf(item);
            string? name = ScalarOrNull(map, "stage") ?? ScalarOrNull(map, "template");
            string? displayName = ScalarOrNull(map, "displayName");
            IReadOnlyList<JobNode> jobs = ParseJobs(map);
            IReadOnlyList<VariableNode> variables = ParseVariables(map);
            string? condition = ScalarOrNull(map, "condition");

            result.Add(new StageNode(name, displayName, jobs, variables, condition, line));
        }

        return result;
    }

    /// <summary>Parses jobs from a pipeline or stage mapping.</summary>
    /// <param name="root">The YAML mapping containing the jobs entry.</param>
    /// <returns>Job nodes in source order.</returns>
    private static List<JobNode> ParseJobs(YamlMappingNode root)
    {
        if (!TryGetSequence(root, "jobs", out YamlSequenceNode? seq))
        {
            return [];
        }

        List<JobNode> result = [];

        foreach (YamlNode item in seq!.Children)
        {
            if (item is not YamlMappingNode map)
            {
                continue;
            }

            int? line = LineOf(item);
            string? name = ScalarOrNull(map, "job") ?? ScalarOrNull(map, "deployment") ?? ScalarOrNull(map, "template");
            string? displayName = ScalarOrNull(map, "displayName");
            int? timeout = IntOrNull(map, "timeoutInMinutes");
            IReadOnlyList<StepNode> steps = ParseSteps(map);
            IReadOnlyList<VariableNode> variables = ParseVariables(map);
            string? condition = ScalarOrNull(map, "condition");

            result.Add(new JobNode(name, displayName, timeout, steps, variables, condition, line));
        }

        return result;
    }

    /// <summary>Parses steps from a pipeline or job mapping.</summary>
    /// <param name="root">The YAML mapping containing the steps entry.</param>
    /// <returns>Step nodes in source order.</returns>
    private static List<StepNode> ParseSteps(YamlMappingNode root)
    {
        if (!TryGetSequence(root, "steps", out YamlSequenceNode? seq))
        {
            return [];
        }

        List<StepNode> result = [];

        foreach (YamlNode item in seq!.Children)
        {
            if (item is not YamlMappingNode map)
            {
                continue;
            }

            int? line = LineOf(item);
            string? task = ScalarOrNull(map, "task");
            string? script = ScalarOrNull(map, "script")
                ?? ScalarOrNull(map, "bash")
                ?? ScalarOrNull(map, "powershell")
                ?? ScalarOrNull(map, "pwsh");
            string? displayName = ScalarOrNull(map, "displayName");
            int? timeout = IntOrNull(map, "timeoutInMinutes");
            bool isCheckout = TryGetNode(map, "checkout", out _);
            string? condition = ScalarOrNull(map, "condition");

            result.Add(new StepNode(task, script, displayName, timeout, isCheckout, condition, line));
        }

        return result;
    }

    /// <summary>Looks up a YAML child node by scalar key.</summary>
    /// <param name="map">The mapping to search.</param>
    /// <param name="key">The key to find.</param>
    /// <param name="node">The matching node, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the key exists.</returns>
    private static bool TryGetNode(YamlMappingNode map, string key, out YamlNode? node)
    {
        foreach (KeyValuePair<YamlNode, YamlNode> entry in map.Children)
        {
            if (entry.Key is YamlScalarNode scalar && scalar.Value == key)
            {
                node = entry.Value;
                return true;
            }
        }

        node = null;
        return false;
    }

    /// <summary>Looks up a child node and verifies that it is a sequence.</summary>
    /// <param name="map">The mapping to search.</param>
    /// <param name="key">The sequence key to find.</param>
    /// <param name="seq">The matching sequence, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the key contains a sequence.</returns>
    private static bool TryGetSequence(
        YamlMappingNode map, string key, out YamlSequenceNode? seq)
    {
        if (TryGetNode(map, key, out YamlNode? node) && node is YamlSequenceNode s)
        {
            seq = s;
            return true;
        }

        seq = null;
        return false;
    }

    /// <summary>Returns a scalar child value, or <see langword="null"/> for non-scalars.</summary>
    /// <param name="map">The mapping to search.</param>
    /// <param name="key">The scalar key to find.</param>
    /// <returns>The scalar value, or <see langword="null"/>.</returns>
    private static string? ScalarOrNull(YamlMappingNode map, string key)
    {
        if (TryGetNode(map, key, out YamlNode? node) && node is YamlScalarNode scalar)
        {
            return scalar.Value;
        }

        return null;
    }

    /// <summary>Parses an integer scalar child value.</summary>
    /// <param name="map">The mapping to search.</param>
    /// <param name="key">The integer key to find.</param>
    /// <returns>The parsed integer, or <see langword="null"/>.</returns>
    private static int? IntOrNull(YamlMappingNode map, string key)
    {
        string? raw = ScalarOrNull(map, key);
        return int.TryParse(raw, out int value) ? value : null;
    }

    /// <summary>Reads a case-insensitive YAML boolean and defaults missing values to false.</summary>
    /// <param name="map">The mapping to search.</param>
    /// <param name="key">The boolean key to find.</param>
    /// <returns>The parsed boolean value.</returns>
    private static bool BoolOrFalse(YamlMappingNode map, string key)
    {
        string? raw = ScalarOrNull(map, key);
        return raw is not null && (raw == "true" || raw == "True" || raw == "TRUE");
    }

    /// <summary>Parses a sequence containing scalar string values.</summary>
    /// <param name="map">The mapping to search.</param>
    /// <param name="key">The sequence key to find.</param>
    /// <returns>Scalar values in source order.</returns>
    private static List<string> ParseScalarList(YamlMappingNode map, string key)
    {
        if (!TryGetSequence(map, key, out YamlSequenceNode? seq))
        {
            return [];
        }

        List<string> result = [];
        foreach (YamlNode item in seq!.Children)
        {
            if (item is YamlScalarNode scalar && scalar.Value is not null)
            {
                result.Add(scalar.Value);
            }
        }

        return result;
    }

    /// <summary>Gets the one-based source line for a YAML node.</summary>
    /// <param name="node">The YAML node.</param>
    /// <returns>The source line, or <see langword="null"/> when unavailable.</returns>
    private static int? LineOf(YamlNode node) =>
        node.Start.Line > 0 ? (int)node.Start.Line : null;
}
