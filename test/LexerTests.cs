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

    [Fact]
    public void Lexer_Identifies_BraceStrings()
    {
        var input = "{simple} {nested {brace} string} {multi\nline}";
        var lexer = new Lexer(input);
        var tokens = lexer.Tokenize();

        Assert.Equal(3, tokens.Count);
        
        Assert.Equal("simple", ((Text)tokens[0].Value!).Content);
        Assert.Equal("nested {brace} string", ((Text)tokens[1].Value!).Content);
        Assert.Equal("multi\nline", ((Text)tokens[2].Value!).Content);
    }

    [Fact]
    public void Lexer_Identifies_Deeply_Nested_BraceStrings()
    {
        var input = "{ a { b { c } d } e }";
        var lexer = new Lexer(input);
        var tokens = lexer.Tokenize();

        Assert.Single(tokens);
        Assert.Equal(" a { b { c } d } e ", ((Text)tokens[0].Value!).Content);
    }

    [Fact]
    public void Lexer_Identifies_BraceStrings_With_Quotes()
    {
        var input = "{He said, \"Hello!\"}";
        var lexer = new Lexer(input);
        var tokens = lexer.Tokenize();

        Assert.Single(tokens);
        Assert.Equal("He said, \"Hello!\"", ((Text)tokens[0].Value!).Content);
    }

    [Fact]
    public void Lexer_Identifies_Multiline_BraceStrings()
    {
        var input = "{\n    Line 1\n    Line 2\n}";
        var lexer = new Lexer(input);
        var tokens = lexer.Tokenize();

        Assert.Single(tokens);
        // The content should include the newlines and leading spaces
        Assert.Equal("\n    Line 1\n    Line 2\n", ((Text)tokens[0].Value!).Content);
    }
}