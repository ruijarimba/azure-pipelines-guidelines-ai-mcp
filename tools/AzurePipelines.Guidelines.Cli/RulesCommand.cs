using System.Collections.Frozen;
using System.CommandLine;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Cli;

/// <summary>
/// The <c>adog rules</c> parent command with <c>list</c> and <c>show</c> subcommands.
/// </summary>
internal static class RulesCommand
{
    private static readonly FrozenSet<string> _validCategories =
        new[] { "general", "jobs", "parameters", "pipelines", "stages", "steps", "variables" }
            .ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    internal static Command Create(IGuidelineRepository repository)
    {
        Command rulesCommand = new("rules", "Browse and query the loaded Azure Pipelines guidelines.");
        rulesCommand.AddCommand(CreateListCommand(repository));
        rulesCommand.AddCommand(CreateShowCommand(repository));
        return rulesCommand;
    }

    // ── rules list ────────────────────────────────────────────────────────────

    private static Command CreateListCommand(IGuidelineRepository repository)
    {
        Option<string?> categoryOpt = new(
            name: "--category",
            description: "Filter by category: general, jobs, parameters, pipelines, stages, steps, variables.",
            getDefaultValue: () => null);

        Option<string> formatOpt = new(
            name: "--format",
            description: "Output format: console (default) or json.",
            getDefaultValue: () => "console");

        Command listCommand = new("list", "List all available guidelines.")
        {
            categoryOpt,
            formatOpt,
        };

        listCommand.SetHandler(
            async (string? category, string format) =>
                Environment.Exit(await RunListAsync(repository, category, format)),
            categoryOpt, formatOpt);

        return listCommand;
    }

    internal static async Task<int> RunListAsync(
        IGuidelineRepository repository,
        string? category,
        string format)
    {
        IReadOnlyList<GuidelineDefinition> guidelines;

        if (category is null)
        {
            guidelines = repository.GetAll();
        }
        else if (TryParseCategory(category, out GuidelineCategory parsed))
        {
            guidelines = repository.GetByCategory(parsed);
        }
        else
        {
            await Console.Error.WriteLineAsync(
                $"error: Unknown category '{category}'. " +
                "Allowed values: general, jobs, parameters, pipelines, stages, steps, variables.")
                .ConfigureAwait(false);
            return ExitCodes.Error;
        }

        string output = format.Equals("json", StringComparison.OrdinalIgnoreCase)
            ? JsonFormatter.FormatGuidelineList(guidelines)
            : ConsoleFormatter.FormatGuidelineList(guidelines);

        Console.Write(output);
        return ExitCodes.Clean;
    }

    // ── rules show ────────────────────────────────────────────────────────────

    private static Command CreateShowCommand(IGuidelineRepository repository)
    {
        Argument<string> idArg = new(
            name: "rule-id",
            description: "The stable guideline identifier, e.g. ADOG-STEPS-001.");

        Option<string> formatOpt = new(
            name: "--format",
            description: "Output format: console (default) or json.",
            getDefaultValue: () => "console");

        Command showCommand = new("show", "Show full details for a specific guideline.")
        {
            idArg,
            formatOpt,
        };

        showCommand.SetHandler(
            async (string id, string format) =>
                Environment.Exit(await RunShowAsync(repository, id, format)),
            idArg, formatOpt);

        return showCommand;
    }

    internal static async Task<int> RunShowAsync(
        IGuidelineRepository repository,
        string id,
        string format)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            await Console.Error.WriteLineAsync("error: rule-id is required.").ConfigureAwait(false);
            return ExitCodes.Error;
        }

        GuidelineId guidelineId;
        try
        {
            guidelineId = new GuidelineId(id);
        }
        catch (ArgumentException)
        {
            await Console.Error.WriteLineAsync(
                $"error: '{id}' is not a valid guideline ID. " +
                "Expected format: ADOG-{{CATEGORY}}-{{NNN}}, e.g. ADOG-STEPS-001.")
                .ConfigureAwait(false);
            return ExitCodes.Error;
        }

        GuidelineDefinition? guideline = repository.FindById(guidelineId);
        if (guideline is null)
        {
            await Console.Error.WriteLineAsync($"error: Guideline '{id}' not found.")
                .ConfigureAwait(false);
            return ExitCodes.Error;
        }

        string output = format.Equals("json", StringComparison.OrdinalIgnoreCase)
            ? JsonFormatter.FormatGuidelineDetail(guideline)
            : ConsoleFormatter.FormatGuidelineDetail(guideline);

        Console.Write(output);
        return ExitCodes.Clean;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool TryParseCategory(string value, out GuidelineCategory result)
    {
        if (!_validCategories.Contains(value))
        {
            result = default;
            return false;
        }

        result = value.ToUpperInvariant() switch
        {
            "GENERAL"    => GuidelineCategory.General,
            "JOBS"       => GuidelineCategory.Jobs,
            "PARAMETERS" => GuidelineCategory.Parameters,
            "PIPELINES"  => GuidelineCategory.Pipelines,
            "STAGES"     => GuidelineCategory.Stages,
            "STEPS"      => GuidelineCategory.Steps,
            "VARIABLES"  => GuidelineCategory.Variables,
            _            => GuidelineCategory.General,
        };

        return true;
    }
}
