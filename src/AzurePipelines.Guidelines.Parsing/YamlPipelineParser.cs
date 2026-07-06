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

    // ── Root loading ───────────────────────────────────────────────────────────

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

    // ── Parameters ────────────────────────────────────────────────────────────

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

    // ── Variables ─────────────────────────────────────────────────────────────

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

    // ── Stages ────────────────────────────────────────────────────────────────

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

    // ── Jobs ──────────────────────────────────────────────────────────────────

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

    // ── Steps ─────────────────────────────────────────────────────────────────

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

    // ── YAML helpers ──────────────────────────────────────────────────────────

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

    private static string? ScalarOrNull(YamlMappingNode map, string key)
    {
        if (TryGetNode(map, key, out YamlNode? node) && node is YamlScalarNode scalar)
        {
            return scalar.Value;
        }

        return null;
    }

    private static int? IntOrNull(YamlMappingNode map, string key)
    {
        string? raw = ScalarOrNull(map, key);
        return int.TryParse(raw, out int value) ? value : null;
    }

    private static bool BoolOrFalse(YamlMappingNode map, string key)
    {
        string? raw = ScalarOrNull(map, key);
        return raw is not null && (raw == "true" || raw == "True" || raw == "TRUE");
    }

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

    private static int? LineOf(YamlNode node) =>
        node.Start.Line > 0 ? (int)node.Start.Line : null;
}
