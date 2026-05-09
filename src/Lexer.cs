using System.Text;
using System.Globalization;

namespace rebelly;

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
        while (Peek() != '"' && _pos < _input.Length)
        {
            sb.Append(Consume());
        }
        Consume(); // Skip closing "
        return new Text(sb.ToString());
    }

    private Value ParseAtom()
    {
        var sb = new StringBuilder();
        while (_pos < _input.Length && !char.IsWhiteSpace(Peek()) && Peek() != '[' && Peek() != ']')
        {
            sb.Append(Consume());
        }

        string raw = sb.ToString();

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

        // 4. It's a regular Word
        return new Word(raw);
    }
}