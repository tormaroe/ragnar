namespace Ragnar;

public class Loader
{
    public Block Load(List<Token> tokens)
    {
        var iterator = tokens.GetEnumerator();
        // We wrap everything in a root block so the whole program 
        // is treated as one executable unit.
        return ParseBlock(iterator, isRoot: true);
    }

    private Block ParseBlock(IEnumerator<Token> tokens, bool isRoot = false)
    {
        var block = new Block();

        while (tokens.MoveNext())
        {
            var token = tokens.Current;

            switch (token.Type)
            {
                case TokenType.Value:
                    // If it's a value (Word, Integer, etc.), just add it.
                    if (token.Value != null) block.Children.Add(token.Value);
                    break;

                case TokenType.OpenBracket:
                    // When we see '[', we recurse to create a nested block.
                    block.Children.Add(ParseBlock(tokens));
                    break;

                case TokenType.CloseBracket:
                    // When we see ']', we are done with this specific block.
                    if (isRoot) throw new Exception("Unexpected ']' at root level.");
                    return block;
            }
        }

        if (!isRoot)
        {
            throw new Exception("Missing closing bracket ']'");
        }

        return block;
    }
}