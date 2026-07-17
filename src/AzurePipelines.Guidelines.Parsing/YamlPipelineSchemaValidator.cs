using AzurePipelines.Guidelines.Core;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace AzurePipelines.Guidelines.Parsing;

/// <summary>
/// Performs deterministic structural validation for a limited Azure Pipelines YAML subset.
/// </summary>
internal sealed class YamlPipelineSchemaValidator : IPipelineSchemaValidator
{
    private static readonly HashSet<string> _pipelineKeys =
    [
        "name", "trigger", "pr", "schedules", "variables", "parameters", "resources",
        "pool", "stages", "jobs", "steps", "extends", "lockBehavior", "appendCommitMessageToRunDescription"
    ];

    public IReadOnlyList<SchemaDiagnostic> Validate(
        string yaml,
        string filePath,
        PipelineSchemaContext context = PipelineSchemaContext.Pipeline)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(yaml);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        List<SchemaDiagnostic> diagnostics = [];
        YamlNode? root = LoadRootNode(yaml, filePath, diagnostics);
        if (root is null)
        {
            return diagnostics;
        }

        switch (context)
        {
            case PipelineSchemaContext.Pipeline:
                if (root is YamlMappingNode pipelineRoot)
                {
                    ValidatePipeline(pipelineRoot, diagnostics);
                }
                else
                {
                    AddRootMappingDiagnostic(diagnostics);
                }
                break;
            case PipelineSchemaContext.Stages:
                ValidateCollectionContext(root, "stages", ValidateStageItem, diagnostics);
                break;
            case PipelineSchemaContext.Jobs:
                ValidateCollectionContext(root, "jobs", ValidateJobItem, diagnostics);
                break;
            case PipelineSchemaContext.Job:
                ValidateSingleItemContext(root, ValidateJobItem, diagnostics);
                break;
            case PipelineSchemaContext.Steps:
                ValidateCollectionContext(root, "steps", ValidateStepItem, diagnostics);
                break;
            case PipelineSchemaContext.Step:
                ValidateSingleItemContext(root, ValidateStepItem, diagnostics);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(context), context, null);
        }

        return diagnostics;
    }

    private static YamlNode? LoadRootNode(
        string yaml,
        string filePath,
        List<SchemaDiagnostic> diagnostics)
    {
        YamlStream stream = new();
        try
        {
            using StringReader reader = new(yaml);
            stream.Load(reader);
        }
        catch (YamlException ex)
        {
            diagnostics.Add(new SchemaDiagnostic("ADOG-SCHEMA-001", $"Failed to parse YAML in '{filePath}': {ex.Message}"));
            return null;
        }

        if (stream.Documents.Count == 0)
        {
            diagnostics.Add(new SchemaDiagnostic("ADOG-SCHEMA-002", $"The file '{filePath}' contains no YAML documents."));
            return null;
        }

        if (stream.Documents.Count > 1)
        {
            diagnostics.Add(new SchemaDiagnostic("ADOG-SCHEMA-004", "Only one YAML document is supported."));
        }

        return stream.Documents[0].RootNode;
    }

    private static void ValidateCollectionContext(
        YamlNode root,
        string property,
        Action<YamlNode, List<SchemaDiagnostic>> itemValidator,
        List<SchemaDiagnostic> diagnostics)
    {
        if (root is YamlSequenceNode)
        {
            ValidateSequence(root, property, itemValidator, diagnostics);
            return;
        }

        if (root is YamlMappingNode map)
        {
            ValidateSequenceProperty(map, property, itemValidator, diagnostics);
            return;
        }

        AddRootMappingOrSequenceDiagnostic(diagnostics);
    }

    private static void ValidateSingleItemContext(
        YamlNode root,
        Action<YamlNode, List<SchemaDiagnostic>> itemValidator,
        List<SchemaDiagnostic> diagnostics)
    {
        if (root is YamlMappingNode)
        {
            itemValidator(root, diagnostics);
        }
        else
        {
            AddRootMappingDiagnostic(diagnostics);
        }
    }

    private static void ValidatePipeline(
        YamlMappingNode root,
        List<SchemaDiagnostic> diagnostics)
    {
        foreach (YamlNode key in root.Children.Keys)
        {
            string? value = ScalarValue(key);
            if (value is not null && !_pipelineKeys.Contains(value))
            {
                diagnostics.Add(new SchemaDiagnostic("ADOG-SCHEMA-005", $"'{value}' is not a recognized pipeline-level property.", LineOf(key)));
            }
        }

        ValidateOptionalSequenceProperty(root, "stages", ValidateStageItem, diagnostics);
        ValidateOptionalSequenceProperty(root, "jobs", ValidateJobItem, diagnostics);
        ValidateOptionalSequenceProperty(root, "steps", ValidateStepItem, diagnostics);
    }

    private static void ValidateStageItem(YamlNode node, List<SchemaDiagnostic> diagnostics)
    {
        if (node is not YamlMappingNode map)
        {
            AddExpectedMapping(diagnostics, "stage", node);
            return;
        }

        ValidateExactlyOneIdentifier(map, ["stage", "template"], "stage or template", diagnostics);
        ValidateOptionalSequenceProperty(map, "jobs", ValidateJobItem, diagnostics);
    }

    private static void ValidateJobItem(YamlNode node, List<SchemaDiagnostic> diagnostics)
    {
        if (node is not YamlMappingNode map)
        {
            AddExpectedMapping(diagnostics, "job", node);
            return;
        }

        ValidateExactlyOneIdentifier(map, ["job", "deployment", "template"], "job, deployment, or template", diagnostics);
        ValidateOptionalSequenceProperty(map, "steps", ValidateStepItem, diagnostics);
    }

    private static void ValidateStepItem(YamlNode node, List<SchemaDiagnostic> diagnostics)
    {
        if (node is not YamlMappingNode map)
        {
            AddExpectedMapping(diagnostics, "step", node);
            return;
        }

        string[] kinds = ["task", "script", "bash", "powershell", "pwsh", "checkout", "template", "download", "publish", "getPackage", "reviewApp"];
        int present = kinds.Count(kind => HasKey(map, kind));
        if (present == 0)
        {
            diagnostics.Add(new SchemaDiagnostic("ADOG-SCHEMA-012", "A step must specify a task, script, checkout, template, or other recognized step action.", LineOf(node)));
        }
        else if (present > 1)
        {
            diagnostics.Add(new SchemaDiagnostic("ADOG-SCHEMA-013", "A step must specify only one step action.", LineOf(node)));
        }
    }

    private static void ValidateExactlyOneIdentifier(
        YamlMappingNode map,
        IReadOnlyList<string> identifiers,
        string description,
        List<SchemaDiagnostic> diagnostics)
    {
        List<string> present = identifiers.Where(identifier => HasKey(map, identifier)).ToList();
        if (present.Count == 0)
        {
            diagnostics.Add(new SchemaDiagnostic("ADOG-SCHEMA-010", $"An item must specify a {description}.", LineOf(map)));
        }
        else if (present.Count > 1)
        {
            diagnostics.Add(new SchemaDiagnostic("ADOG-SCHEMA-011", $"An item must specify only one of: {string.Join(", ", identifiers)}.", LineOf(map)));
        }
        else if (map.Children.First(entry => ScalarValue(entry.Key) == present[0]).Value is not YamlScalarNode)
        {
            diagnostics.Add(new SchemaDiagnostic("ADOG-SCHEMA-014", $"The '{present[0]}' value must be a scalar.", LineOf(map)));
        }
    }

    private static void ValidateOptionalSequenceProperty(
        YamlMappingNode map,
        string property,
        Action<YamlNode, List<SchemaDiagnostic>> itemValidator,
        List<SchemaDiagnostic> diagnostics)
    {
        if (TryGetNode(map, property, out YamlNode? node))
        {
            ValidateSequence(node!, property, itemValidator, diagnostics);
        }
    }

    private static void ValidateSequenceProperty(
        YamlMappingNode map,
        string property,
        Action<YamlNode, List<SchemaDiagnostic>> itemValidator,
        List<SchemaDiagnostic> diagnostics)
    {
        if (!TryGetNode(map, property, out YamlNode? node))
        {
            diagnostics.Add(new SchemaDiagnostic("ADOG-SCHEMA-006", $"The document must contain '{property}:' as a sequence.", LineOf(map)));
            return;
        }

        ValidateSequence(node!, property, itemValidator, diagnostics);
    }

    private static void ValidateSequence(
        YamlNode node,
        string property,
        Action<YamlNode, List<SchemaDiagnostic>> itemValidator,
        List<SchemaDiagnostic> diagnostics)
    {
        if (node is not YamlSequenceNode sequence)
        {
            diagnostics.Add(new SchemaDiagnostic("ADOG-SCHEMA-007", $"'{property}:' must be a sequence.", LineOf(node)));
            return;
        }

        foreach (YamlNode item in sequence.Children)
        {
            itemValidator(item, diagnostics);
        }
    }

    private static void AddExpectedMapping(List<SchemaDiagnostic> diagnostics, string context, YamlNode node) =>
        diagnostics.Add(new SchemaDiagnostic("ADOG-SCHEMA-008", $"Each {context} item must be a mapping.", LineOf(node)));

    private static void AddRootMappingDiagnostic(List<SchemaDiagnostic> diagnostics) =>
        diagnostics.Add(new SchemaDiagnostic("ADOG-SCHEMA-003", "The YAML document root must be a mapping."));

    private static void AddRootMappingOrSequenceDiagnostic(List<SchemaDiagnostic> diagnostics) =>
        diagnostics.Add(new SchemaDiagnostic("ADOG-SCHEMA-003", "The YAML document root must be a mapping or sequence."));

    private static bool HasKey(YamlMappingNode map, string key) =>
        TryGetNode(map, key, out _);

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

    private static string? ScalarValue(YamlNode node) =>
        (node as YamlScalarNode)?.Value;

    private static int? LineOf(YamlNode node) =>
        node.Start.Line > 0 ? (int)node.Start.Line : null;
}
