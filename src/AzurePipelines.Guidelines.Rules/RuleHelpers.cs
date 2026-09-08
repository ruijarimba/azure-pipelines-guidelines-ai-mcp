using System.Text;

namespace AzurePipelines.Guidelines.Rules;

/// <summary>
/// Shared utility methods used by multiple rule implementations.
/// </summary>
internal static class RuleHelpers
{
    private static int DiagnosticValueMaxLength => 200;

    /// <summary>
    /// Returns the one-based line number for the given character index in
    /// <paramref name="content"/>.
    /// </summary>
    internal static int GetLineNumber(string content, int charIndex)
    {
        int line = 1;
        for (int i = 0; i < charIndex && i < content.Length; i++)
        {
            if (content[i] == '\n')
            {
                line++;
            }
        }

        return line;
    }

    /// <summary>
    /// Removes control characters and limits untrusted pipeline text before it is
    /// included in a diagnostic message.
    /// </summary>
    internal static string SanitizeForDiagnostic(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        StringBuilder builder = new(value.Length);
        foreach (char character in value)
        {
            if (!char.IsControl(character))
            {
                builder.Append(character);
            }
        }

        string sanitized = builder.ToString().Trim();
        if (sanitized.Length <= DiagnosticValueMaxLength)
        {
            return sanitized;
        }

        return string.Concat(sanitized.Substring(0, DiagnosticValueMaxLength), "\u2026");
    }
}
