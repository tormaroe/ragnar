namespace Ragnar.Tests;

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

    [Fact]
    public void Lexer_Identifies_LitWords()
    {
        var lexer = new Lexer("'apple 'orange:");
        var tokens = lexer.Tokenize();
        
        Assert.NotEmpty(tokens);
        var litWord = Assert.IsType<LitWord>(tokens[0].Value); // Assert.IsType handles the null check
        Assert.Equal("apple", litWord.Name);
    }

    [Fact]
    public void Lexer_Identifies_New_Types()
    {
        var lexer = new Lexer("%test.txt /wait call/wait");
        var tokens = lexer.Tokenize();

        // 1. File
        var fileToken = Assert.IsType<File>(tokens[0].Value);
        Assert.Equal("test.txt", fileToken.Path);

        // 2. Refinement
        var refToken = Assert.IsType<Refinement>(tokens[1].Value);
        Assert.Equal("wait", refToken.Name);

        // 3. Path
        var pathToken = Assert.IsType<Path>(tokens[2].Value);
        Assert.Equal(2, pathToken.Parts.Count);
        Assert.Equal("call", ((Word)pathToken.Parts[0]).Name);
        Assert.Equal("wait", ((Word)pathToken.Parts[1]).Name);
    }
}