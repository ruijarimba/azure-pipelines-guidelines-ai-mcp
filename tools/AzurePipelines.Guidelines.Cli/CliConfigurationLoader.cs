using System.Text.Json;
using System.Text.Json.Serialization;

namespace AzurePipelines.Guidelines.Cli;

/// <summary>
/// Loads optional CLI configuration from the current directory or user profile.
/// </summary>
internal static class CliConfigurationLoader
{
    internal static CliConfiguration Load()
    {
        string? configPath = FindConfigFile();
        if (string.IsNullOrWhiteSpace(configPath))
        {
            return CliConfiguration.Empty;
        }

        try
        {
            string content = File.ReadAllText(configPath);
            using JsonDocument document = JsonDocument.Parse(content);

            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return CliConfiguration.Empty;
            }

            CliConfiguration configuration = new();
            if (document.RootElement.TryGetProperty("format", out JsonElement formatElement))
            {
                configuration.Format = GetStringValue(formatElement);
            }

            if (document.RootElement.TryGetProperty("severity", out JsonElement severityElement))
            {
                configuration.Severity = GetStringValue(severityElement);
            }

            if (document.RootElement.TryGetProperty("category", out JsonElement categoryElement))
            {
                configuration.Category = GetStringValue(categoryElement);
            }

            if (document.RootElement.TryGetProperty("output", out JsonElement outputElement))
            {
                configuration.Output = GetStringValue(outputElement);
            }

            if (document.RootElement.TryGetProperty("soft-fail", out JsonElement softFailElement))
            {
                configuration.SoftFail = GetStringValue(softFailElement);
            }

            if (document.RootElement.TryGetProperty("no-color", out JsonElement noColorElement))
            {
                configuration.NoColor = GetStringValue(noColorElement);
            }

            if (document.RootElement.TryGetProperty("quiet", out JsonElement quietElement))
            {
                configuration.Quiet = GetStringValue(quietElement);
            }

            if (document.RootElement.TryGetProperty("verbose", out JsonElement verboseElement))
            {
                configuration.Verbose = GetStringValue(verboseElement);
            }

            if (!configuration.TryValidate(out string? validationError))
            {
                configuration.ErrorMessage = validationError;
                return configuration;
            }

            return configuration;
        }
        catch (JsonException ex)
        {
            return new CliConfiguration
            {
                ErrorMessage = $"error: Invalid JSON in config file {configPath}: {ex.Message}",
            };
        }
        catch (IOException ex)
        {
            return new CliConfiguration
            {
                ErrorMessage = $"error: Cannot read config file {configPath}: {ex.Message}",
            };
        }
    }

    private static string? FindConfigFile()
    {
        foreach (string candidate in GetCandidatePaths())
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string? GetStringValue(JsonElement element)
    {
        return element.ValueKind == JsonValueKind.String ? element.GetString() : element.ToString();
    }

    private static List<string> GetCandidatePaths()
    {
        List<string> candidates = [];

        string? currentDirectory = Environment.CurrentDirectory;
        if (!string.IsNullOrWhiteSpace(currentDirectory))
        {
            candidates.Add(Path.Combine(currentDirectory, "adog.json"));
            candidates.Add(Path.Combine(currentDirectory, ".adogrc.json"));
        }

        string? homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(homeDirectory))
        {
            candidates.Add(Path.Combine(homeDirectory, "adog.json"));
            candidates.Add(Path.Combine(homeDirectory, ".adogrc.json"));
        }

        return candidates;
    }
}

/// <summary>
/// Stores values read from an <c>adog.json</c> configuration file.
/// </summary>
internal sealed class CliConfiguration
{
    internal static CliConfiguration Empty { get; } = new();

    [JsonPropertyName("format")]
    [JsonInclude]
    internal string? Format { get; set; }

    [JsonPropertyName("severity")]
    [JsonInclude]
    internal string? Severity { get; set; }

    [JsonPropertyName("category")]
    [JsonInclude]
    internal string? Category { get; set; }

    [JsonPropertyName("output")]
    [JsonInclude]
    internal string? Output { get; set; }

    [JsonPropertyName("soft-fail")]
    [JsonInclude]
    internal string? SoftFail { get; set; }

    [JsonPropertyName("no-color")]
    [JsonInclude]
    internal string? NoColor { get; set; }

    [JsonPropertyName("quiet")]
    [JsonInclude]
    internal string? Quiet { get; set; }

    [JsonPropertyName("verbose")]
    [JsonInclude]
    internal string? Verbose { get; set; }

    [JsonIgnore]
    internal string? ErrorMessage { get; set; }

    internal string? GetFormatValue() => Format;

    internal string? GetSeverityValue() => Severity;

    internal string? GetCategoryValue() => Category;

    internal string? GetOutputValue() => Output;

    internal bool? GetSoftFailValue() => ParseBoolean(SoftFail);

    internal bool? GetNoColorValue() => ParseBoolean(NoColor);

    internal bool? GetQuietValue() => ParseBoolean(Quiet);

    internal bool? GetVerboseValue() => ParseBoolean(Verbose);

    internal bool TryValidate(out string? errorMessage)
    {
        errorMessage = null;

        if (!TryParseBoolean(SoftFail, out _))
        {
            errorMessage = "error: Invalid boolean value for config property 'soft-fail'. Allowed values: true/false, 1/0, yes/no.";
            return false;
        }

        if (!TryParseBoolean(NoColor, out _))
        {
            errorMessage = "error: Invalid boolean value for config property 'no-color'. Allowed values: true/false, 1/0, yes/no.";
            return false;
        }

        if (!TryParseBoolean(Quiet, out _))
        {
            errorMessage = "error: Invalid boolean value for config property 'quiet'. Allowed values: true/false, 1/0, yes/no.";
            return false;
        }

        if (!TryParseBoolean(Verbose, out _))
        {
            errorMessage = "error: Invalid boolean value for config property 'verbose'. Allowed values: true/false, 1/0, yes/no.";
            return false;
        }

        return true;
    }

    private static bool? ParseBoolean(string? value)
    {
        return TryParseBoolean(value, out bool? parsedValue) ? parsedValue : null;
    }

    private static bool TryParseBoolean(string? value, out bool? parsedValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            parsedValue = null;
            return true;
        }

        switch (value.Trim().ToUpperInvariant())
        {
            case "TRUE":
            case "1":
            case "YES":
                parsedValue = true;
                return true;
            case "FALSE":
            case "0":
            case "NO":
                parsedValue = false;
                return true;
            default:
                parsedValue = null;
                return false;
        }
    }
}
