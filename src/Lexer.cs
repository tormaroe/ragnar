using System.Text;
using System.Globalization;

namespace Ragnar;

public class Lexer(string input)
{
    private readonly string _input = input;
    private int _pos = 0;

    private char Peek() => _pos < _input.Length ? _input[_pos] : '\0';
    private char Consume() => _pos < _input.Length ? _input[_pos++] : '\0';

    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();

        while (_pos < _input.Length)
        {
            char c = Peek();

            if (char.IsWhiteSpace(c))
            {
                Consume();
            }
            else if (c == ';')
            {
                // Consume until the end of the line
                while (_pos < _input.Length && Peek() != '\n' && Peek() != '\r')
                {
                    Consume();
                }
                // The newline itself will be handled by the IsWhiteSpace check 
                // on the next iteration of the outer loop.
            }
            else if (c == '[')
            {
                Consume();
                tokens.Add(new Token(TokenType.OpenBracket));
            }
            else if (c == ']')
            {
                Consume();
                tokens.Add(new Token(TokenType.CloseBracket));
            }
            else if (c == '(')
            {
                Consume();
                tokens.Add(new Token(TokenType.OpenParen));
            }
            else if (c == ')')
            {
                Consume();
                tokens.Add(new Token(TokenType.CloseParen));
            }
            else if (c == '"')
            {
                tokens.Add(new Token(TokenType.Value, ParseString()));
            }
            else
            {
                tokens.Add(new Token(TokenType.Value, ParseAtom()));
            }
        }

        return tokens;
    }

    private Text ParseString()
    {
        Consume(); // Skip opening "
        var sb = new StringBuilder();
        while (_pos < _input.Length && Peek() != '"')
        {
            sb.Append(Consume());
        }

        if (_pos >= _input.Length)
            throw new IncompleteInputException("Unclosed string");

        Consume(); // Skip closing "
        return new Text(sb.ToString());
    }

    private Value ParseAtom()
    {
        var sb = new StringBuilder();
        while (_pos < _input.Length && !char.IsWhiteSpace(Peek()) && 
                Peek() != '[' && Peek() != ']' && 
                Peek() != '(' && Peek() != ')')
        {
            sb.Append(Consume());
        }

        string raw = sb.ToString();

        // 1. File Type (Rebol uses % for files)
        if (raw.StartsWith('%'))
        {
            return new File(raw);
        }

        // 2. Refinement or Path
        if (raw.Contains('/') && raw != "/")
        {
            bool isSetPath = raw.EndsWith(':');
            string pathContent = isSetPath ? raw.TrimEnd(':') : raw;

            // Handle Refinement vs Path logic as before...
            if (!isSetPath && pathContent.StartsWith('/') && !pathContent.Substring(1).Contains('/'))
            {
                return new Refinement(pathContent);
            }

            var segments = ParsePathSegments(pathContent);
            return isSetPath ? new SetPath(segments) : new Path(segments);
        }

        // 1. Try Integer / Decimal (existing logic)
        if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out long i))
            return new Integer(i);

        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
            return new Decimal(d);

        // 2. New: Check for Set-Word (ends with :)
        if (raw.EndsWith(':') && raw.Length > 1)
            return new SetWord(raw);

        // 3. New: Check for Get-Word (starts with :)
        if (raw.StartsWith(':') && raw.Length > 1)
            return new GetWord(raw);

        if (raw.StartsWith('\'') && raw.Length > 1)
            return new LitWord(raw);

        // 4. It's a regular Word
        return new Word(raw);
    }

    private List<Value> ParsePathSegments(string raw)
    {
        // Split by slash: "call/wait" -> ["call", "wait"]
        var segments = raw.Split('/');
        var parts = new List<Value>();

        foreach (var seg in segments)
        {
            if (string.IsNullOrEmpty(seg)) continue;

            // Recursively determine what each segment is.
            // Most are Words, but "data/1" has an Integer at the end.
            if (long.TryParse(seg, out long intVal))
                parts.Add(new Integer(intVal));
            else
                parts.Add(new Word(seg));
        }
        return parts;
    }
}