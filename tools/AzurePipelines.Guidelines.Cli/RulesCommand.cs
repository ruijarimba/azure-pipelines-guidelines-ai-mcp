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

    private static readonly FrozenSet<string> _validSeverities =
        new[] { "do", "do-not", "avoid", "consider" }
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
        Option<string[]?> categoryOpt = new(
            name: "--category",
            description: "Filter by one or more categories: general, jobs, parameters, pipelines, stages, steps, variables.",
            getDefaultValue: () => null);

        Option<string[]?> severityOpt = new(
            aliases: ["--severity", "--guideline-severity"],
            description: "Filter by one or more guideline severities: do, do-not, avoid, consider. --severity remains supported as a compatibility alias.",
            getDefaultValue: () => null);

        Option<string> formatOpt = new(
            name: "--format",
            description: "Output format: console (default) or json.",
            getDefaultValue: () => "console");

        Command listCommand = new("list", "List all available guidelines.")
        {
            categoryOpt,
            severityOpt,
            formatOpt,
        };

        listCommand.SetHandler(
            async (string[]? category, string[]? severity, string format) =>
                Environment.Exit(await RunListAsync(repository, category, severity, format)),
            categoryOpt, severityOpt, formatOpt);

        return listCommand;
    }

    internal static async Task<int> RunListAsync(
        IGuidelineRepository repository,
        string[]? category = null,
        string[]? severity = null,
        string format = "console")
    {
        IReadOnlyList<GuidelineDefinition> guidelines;

        if (category is null || category.Length == 0)
        {
            guidelines = repository.GetAll();
        }
        else if (category.Length == 1 && TryParseCategory(category[0], out GuidelineCategory parsedCategory))
        {
            guidelines = repository.GetByCategory(parsedCategory);
        }
        else
        {
            List<GuidelineCategory> parsedCategories = [];
            foreach (string categoryValue in category)
            {
                foreach (string part in SplitValues(categoryValue))
                {
                    if (!TryParseCategory(part, out GuidelineCategory parsed))
                    {
                        await Console.Error.WriteLineAsync(
                            $"error: Unknown category '{part}'. " +
                            "Allowed values: general, jobs, parameters, pipelines, stages, steps, variables.")
                            .ConfigureAwait(false);
                        return ExitCodes.Error;
                    }

                    parsedCategories.Add(parsed);
                }
            }

            guidelines = [.. repository.GetAll().Where(g => parsedCategories.Contains(g.Category))];
        }

        if (severity is { Length: > 0 })
        {
            List<GuidelineSeverity> parsedSeverities = [];
            foreach (string severityValue in severity)
            {
                foreach (string part in SplitValues(severityValue))
                {
                    if (!TryParseSeverity(part, out GuidelineSeverity parsedSeverity))
                    {
                        await Console.Error.WriteLineAsync(
                            $"error: Unknown severity '{part}'. " +
                            "Allowed values: do, do-not, avoid, consider.")
                            .ConfigureAwait(false);
                        return ExitCodes.Error;
                    }

                    parsedSeverities.Add(parsedSeverity);
                }
            }

            guidelines = [.. guidelines.Where(g => parsedSeverities.Contains(g.Severity))];
        }

        string output = format.Equals("json", StringComparison.OrdinalIgnoreCase)
            ? JsonFormatter.FormatGuidelineList(guidelines)
            : ConsoleFormatter.FormatGuidelineList(guidelines);

        Console.Write(output);
        return ExitCodes.Success;
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
        return ExitCodes.Success;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string[] SplitValues(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

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

    private static bool TryParseSeverity(string value, out GuidelineSeverity result)
    {
        if (!_validSeverities.Contains(value))
        {
            result = default;
            return false;
        }

        result = value.ToUpperInvariant() switch
        {
            "DO"      => GuidelineSeverity.Do,
            "DO-NOT"  => GuidelineSeverity.DoNot,
            "AVOID"   => GuidelineSeverity.Avoid,
            "CONSIDER" => GuidelineSeverity.Consider,
            _          => GuidelineSeverity.Consider,
        };

        return true;
    }
}
