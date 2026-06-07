using System;
using System.Collections.Generic;
using System.Text;

namespace Ragnar;

public static class ReplHighlighter
{
    private static void Write(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.Write(text);
    }

    private static bool IsNumber(string raw)
    {
        return long.TryParse(raw, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out _) ||
               double.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _);
    }

    private static bool IsKeyword(string raw)
    {
        return raw == "true" || raw == "false" || raw == "none";
    }

    private static void WriteAtom(string raw, Context? context)
    {
        if (raw.StartsWith('%'))
        {
            Write(raw, ReplConfig.FileColor);
        }
        else if (raw.Contains('/') && raw != "/" && raw != "//")
        {
            // Path segment
            int firstSlash = raw.IndexOf('/');
            string firstSegment = raw.Substring(0, firstSlash);
            string rest = raw.Substring(firstSlash);

            // Check if firstSegment is a function/native
            if (context != null && context.TryGet(firstSegment, out var val) && val != null && (val is Native || val is Function))
            {
                Write(firstSegment, ReplConfig.FunctionColor);
            }
            else if (IsKeyword(firstSegment))
            {
                Write(firstSegment, ReplConfig.KeywordColor);
            }
            else if (IsNumber(firstSegment))
            {
                Write(firstSegment, ReplConfig.NumberColor);
            }
            else
            {
                Write(firstSegment, ReplConfig.InputColor);
            }

            // Write the rest as segments
            Write(rest, ReplConfig.InputColor);
        }
        else
        {
            // Regular atom
            if (raw.StartsWith(':') && raw.Length > 1)
            {
                Write(raw, ReplConfig.GetWordColor);
            }
            else if (raw.EndsWith(':') && raw.Length > 1)
            {
                Write(raw, ReplConfig.SetWordColor);
            }
            else if (raw.StartsWith('\'') && raw.Length > 1)
            {
                Write(raw, ReplConfig.LitWordColor);
            }
            else if (IsNumber(raw))
            {
                Write(raw, ReplConfig.NumberColor);
            }
            else if (IsKeyword(raw))
            {
                Write(raw, ReplConfig.KeywordColor);
            }
            else if (context != null && context.TryGet(raw, out var val) && val != null && (val is Native || val is Function))
            {
                Write(raw, ReplConfig.FunctionColor);
            }
            else
            {
                Write(raw, ReplConfig.InputColor);
            }
        }
    }

    public static void WriteColored(string text, Context? context)
    {
        var originalColor = Console.ForegroundColor;
        int pos = 0;

        char Peek() => pos < text.Length ? text[pos] : '\0';
        char PeekNext() => pos + 1 < text.Length ? text[pos + 1] : '\0';
        char Consume() => pos < text.Length ? text[pos++] : '\0';

        while (pos < text.Length)
        {
            char c = Peek();

            if (char.IsWhiteSpace(c))
            {
                Write(Consume().ToString(), ReplConfig.InputColor);
            }
            else if (c == ';')
            {
                var sb = new StringBuilder();
                while (pos < text.Length && Peek() != '\n' && Peek() != '\r')
                {
                    sb.Append(Consume());
                }
                Write(sb.ToString(), ReplConfig.CommentColor);
            }
            else if (c == '[' || c == ']' || c == '(' || c == ')')
            {
                Write(Consume().ToString(), ReplConfig.BracketColor);
            }
            else if (c == '#' && PeekNext() == '"')
            {
                var sb = new StringBuilder();
                sb.Append(Consume()); // #
                sb.Append(Consume()); // "
                while (pos < text.Length)
                {
                    char next = Consume();
                    sb.Append(next);
                    if (next == '"') break;
                }
                Write(sb.ToString(), ReplConfig.CharacterColor);
            }
            else if (c == '"')
            {
                var sb = new StringBuilder();
                sb.Append(Consume()); // "
                while (pos < text.Length)
                {
                    char next = Peek();
                    if (next == '"')
                    {
                        sb.Append(Consume());
                        break;
                    }
                    else if (next == '\\')
                    {
                        sb.Append(Consume()); // \
                        if (pos < text.Length)
                        {
                            sb.Append(Consume()); // escaped char
                        }
                    }
                    else
                    {
                        sb.Append(Consume());
                    }
                }
                Write(sb.ToString(), ReplConfig.StringColor);
            }
            else if (c == '{')
            {
                var sb = new StringBuilder();
                sb.Append(Consume()); // {
                int depth = 1;
                while (pos < text.Length)
                {
                    char next = Consume();
                    sb.Append(next);
                    if (next == '{')
                    {
                        depth++;
                    }
                    else if (next == '}')
                    {
                        depth--;
                        if (depth == 0) break;
                    }
                }
                Write(sb.ToString(), ReplConfig.StringColor);
            }
            else
            {
                var sb = new StringBuilder();
                while (pos < text.Length && !char.IsWhiteSpace(Peek()) &&
                       Peek() != ';' &&
                       Peek() != '[' && Peek() != ']' &&
                       Peek() != '(' && Peek() != ')')
                {
                    sb.Append(Consume());
                }
                WriteAtom(sb.ToString(), context);
            }
        }

        Console.ForegroundColor = originalColor;
    }
}
