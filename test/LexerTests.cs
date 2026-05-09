namespace rebelly.tests;

public class LexerTests
{
    [Fact]
    public void Tokenize_MixedTypes_ReturnsCorrectTokens()
    {
        var input = "name: :name 42 3.14 \"hello\" [ ]";
        var lexer = new Lexer(input);
        var tokens = lexer.Tokenize();

        Assert.Equal(7, tokens.Count);
        Assert.IsType<SetWord>(tokens[0].Value);
        Assert.IsType<GetWord>(tokens[1].Value);
        Assert.IsType<Integer>(tokens[2].Value);
        Assert.IsType<Decimal>(tokens[3].Value);
        Assert.IsType<Text>(tokens[4].Value);
        Assert.Equal(TokenType.OpenBracket, tokens[5].Type);
        Assert.Equal(TokenType.CloseBracket, tokens[6].Type);
    }
}