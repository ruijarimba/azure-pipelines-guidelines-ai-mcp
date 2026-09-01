using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AzurePipelines.Guidelines.Core;

namespace AzurePipelines.Guidelines.Rules;

/// <summary>
/// ADOG-STEPS-001 (avoid): Detects macro-syntax variable references (<c>$(VAR)</c>)
/// in pipeline YAML. Macro syntax expands at queue time and can fail silently when a
/// variable is undefined. Consider using runtime expressions (<c>$[variables.VAR]</c>)
/// instead.
/// </summary>
[RuleMetadata("ADOG-STEPS-001", "https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/avoid-pipeline-variables.md")]
internal sealed partial class MacroSyntaxInStepsRule : IGuidelineRule
{
    [GeneratedRegex(
        @"\$\([A-Za-z0-9_.]+\)",
        RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex MacroPattern();

    [GeneratedRegex(
        @"^[ \t]*(?:-\s*)?template\s*:",
        RegexOptions.CultureInvariant)]
    private static partial Regex TemplateDeclarationPattern();

    [GeneratedRegex(
        @"^[ \t]*parameters\s*:",
        RegexOptions.CultureInvariant)]
    private static partial Regex ParametersMappingPattern();

    private static readonly GuidelineId _id = new("ADOG-STEPS-001");

    /// <inheritdoc/>
    public GuidelineId GuidelineId => _id;

    /// <inheritdoc/>
    public async IAsyncEnumerable<Diagnostic> EvaluateAsync(
        PipelineDocument document,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        foreach (Match match in MacroPattern().Matches(document.CommentFreeContent))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsTemplateParameterValue(document.CommentFreeContent, match.Index))
            {
                continue;
            }

            int line = RuleHelpers.GetLineNumber(document.CommentFreeContent, match.Index);
            string macroReference = RuleHelpers.SanitizeForDiagnostic(match.Value);

            yield return new Diagnostic(
                _id,
                DiagnosticSeverity.Warning,
                $"Macro-syntax reference '{macroReference}' found. " +
                "Avoid $(VAR) macro syntax; prefer runtime expressions $[variables.VAR] " +
                "or template expressions ${{ variables.VAR }}.",
                document.FilePath,
                line,
                Column: null);
        }
    }

    private static bool IsTemplateParameterValue(string content, int matchIndex)
    {
        int matchLineStart = GetLineStart(content, matchIndex);
        int matchIndentation = GetIndentation(content, matchLineStart);
        int parametersLineStart = FindParametersMapping(content, matchLineStart, matchIndentation);

        return parametersLineStart >= 0 && HasTemplateDeclaration(content, parametersLineStart);
    }

    private static int FindParametersMapping(string content, int lineStart, int lineIndentation)
    {
        for (int currentLineStart = GetPreviousLineStart(content, lineStart);
             currentLineStart >= 0;
             currentLineStart = GetPreviousLineStart(content, currentLineStart))
        {
            int currentIndentation = GetIndentation(content, currentLineStart);
            if (currentIndentation >= lineIndentation)
            {
                continue;
            }

            string line = GetLine(content, currentLineStart);
            return ParametersMappingPattern().IsMatch(line) ? currentLineStart : -1;
        }

        return -1;
    }

    private static bool HasTemplateDeclaration(string content, int parametersLineStart)
    {
        int parametersIndentation = GetIndentation(content, parametersLineStart);

        for (int currentLineStart = GetPreviousLineStart(content, parametersLineStart);
             currentLineStart >= 0;
             currentLineStart = GetPreviousLineStart(content, currentLineStart))
        {
            int currentIndentation = GetIndentation(content, currentLineStart);
            if (currentIndentation > parametersIndentation)
            {
                continue;
            }

            return TemplateDeclarationPattern().IsMatch(GetLine(content, currentLineStart));
        }

        return false;
    }

    private static int GetLineStart(string content, int index)
    {
        int lineStart = content.LastIndexOf('\n', Math.Max(0, index - 1));
        return lineStart < 0 ? 0 : lineStart + 1;
    }

    private static int GetPreviousLineStart(string content, int lineStart)
    {
        if (lineStart == 0)
        {
            return -1;
        }

        return GetLineStart(content, lineStart - 1);
    }

    private static int GetIndentation(string content, int lineStart)
    {
        int indentation = 0;
        while (lineStart + indentation < content.Length &&
               (content[lineStart + indentation] is ' ' or '\t'))
        {
            indentation++;
        }

        return indentation;
    }

    private static string GetLine(string content, int lineStart)
    {
        int lineEnd = content.IndexOf('\n', lineStart);
        lineEnd = lineEnd < 0 ? content.Length : lineEnd;
        return content[lineStart..lineEnd].TrimEnd('\r');
    }
}
