namespace AzurePipelines.Guidelines.Rules;

/// <summary>
/// Shared utility methods used by multiple rule implementations.
/// </summary>
internal static class RuleHelpers
{
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
}
