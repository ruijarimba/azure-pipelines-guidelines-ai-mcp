namespace AzurePipelines.Guidelines.Parsing;

internal static class CommentFreeContentBuilder
{
    internal static string Build(string content)
    {
        char[] result = content.ToCharArray();
        int blockScalarIndentation = -1;

        for (int lineStart = 0; lineStart < content.Length;)
        {
            int lineEnd = GetLineEnd(content, lineStart);
            int contentEnd = GetContentEnd(content, lineStart, lineEnd);
            int firstContent = FindFirstContent(content, lineStart, contentEnd);
            int indentation = firstContent - lineStart;

            if (blockScalarIndentation >= 0 && firstContent < contentEnd &&
                indentation <= blockScalarIndentation)
            {
                blockScalarIndentation = -1;
            }

            if (blockScalarIndentation >= 0 && firstContent < contentEnd)
            {
                if (content[firstContent] == '#')
                {
                    Mask(result, firstContent, contentEnd);
                }
            }
            else if (firstContent < contentEnd && content[firstContent] == '#')
            {
                Mask(result, firstContent, contentEnd);
            }
            else
            {
                blockScalarIndentation = MaskInlineComment(
                    content, result, lineStart, contentEnd, indentation);
            }

            lineStart = lineEnd < content.Length ? lineEnd + 1 : content.Length;
        }

        return new(result);
    }

    private static int MaskInlineComment(
        string content,
        char[] result,
        int lineStart,
        int contentEnd,
        int indentation)
    {
        bool inSingleQuote = false;
        bool inDoubleQuote = false;
        int blockScalarIndentation = -1;

        for (int index = lineStart; index < contentEnd; index++)
        {
            char current = content[index];
            if (current == '\\' && inDoubleQuote)
            {
                index++;
                continue;
            }

            if (current == '\'' && !inDoubleQuote)
            {
                inSingleQuote = !inSingleQuote;
                continue;
            }

            if (current == '"' && !inSingleQuote)
            {
                inDoubleQuote = !inDoubleQuote;
                continue;
            }

            if (!inSingleQuote && !inDoubleQuote && current == '#'
                && (index == lineStart || char.IsWhiteSpace(content[index - 1])))
            {
                Mask(result, index, contentEnd);
                break;
            }

            if (!inSingleQuote && !inDoubleQuote && IsBlockScalarIndicator(content, index, contentEnd))
            {
                blockScalarIndentation = indentation;
            }
        }

        return blockScalarIndentation;
    }

    private static bool IsBlockScalarIndicator(string content, int index, int contentEnd)
    {
        char current = content[index];
        if (current is not ('|' or '>') || (index > 0 && !char.IsWhiteSpace(content[index - 1])))
        {
            return false;
        }

        int next = index + 1;
        return next >= contentEnd || content[next] is '-' or '+' or >= '0' and <= '9' ||
            char.IsWhiteSpace(content[next]);
    }

    private static int GetLineEnd(string content, int lineStart)
    {
        int lineEnd = content.IndexOf('\n', lineStart);
        return lineEnd < 0 ? content.Length : lineEnd;
    }

    private static int GetContentEnd(string content, int lineStart, int lineEnd) =>
        lineEnd > lineStart && content[lineEnd - 1] == '\r' ? lineEnd - 1 : lineEnd;

    private static int FindFirstContent(string content, int lineStart, int contentEnd)
    {
        int index = lineStart;
        while (index < contentEnd && content[index] is ' ' or '\t')
        {
            index++;
        }

        return index;
    }

    private static void Mask(char[] content, int start, int end)
    {
        Array.Fill(content, ' ', start, end - start);
    }
}
