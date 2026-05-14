namespace Ragnar;

public class Loader
{
    public Block Load(List<Token> tokens)
    {
        var iterator = tokens.GetEnumerator();
        // The root is just a block that doesn't have a closing token
        return new Block(ParseChildren(iterator, null));
    }

    private List<Value> ParseChildren(IEnumerator<Token> tokens, TokenType? endType)
    {
        var children = new List<Value>();

        while (tokens.MoveNext())
        {
            var token = tokens.Current;

            // If we hit the token we are looking for (like ']' or ')'), return the list
            if (endType.HasValue && token.Type == endType.Value)
            {
                return children;
            }

            switch (token.Type)
            {
                case TokenType.Value:
                    if (token.Value != null) children.Add(token.Value);
                    break;

                case TokenType.OpenBracket:
                    // Recurse: Start a new block and look for a CloseBracket
                    children.Add(new Block(ParseChildren(tokens, TokenType.CloseBracket)));
                    break;

                case TokenType.OpenParen:
                    // Recurse: Start a new Paren and look for a CloseParen
                    children.Add(new Paren(ParseChildren(tokens, TokenType.CloseParen)));
                    break;

                case TokenType.CloseBracket:
                case TokenType.CloseParen:
                    // If we hit a closing tag we weren't expecting, it's an error
                    throw new Exception($"Unexpected closing token: {token.Type}");
            }
        }

        // If the loop ends but we were still looking for a closing token...
        if (endType.HasValue)
        {
            throw new IncompleteInputException($"Missing closing token for {endType.Value}");
        }

        return children;
    }
}