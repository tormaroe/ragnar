using Xunit;
using Ragnar;
using System;

namespace Ragnar.Tests;

public class StringTests
{
    private (Value Result, Context Context) Run(string code)
    {
        var lexer = new Lexer(code);
        var tokens = lexer.Tokenize();
        var loader = new Loader();
        var root = loader.Load(tokens);

        var ctx = Runtime.CreateGlobalContext();
        var interpreter = new Interpreter();

        var result = interpreter.Evaluate(root, ctx);
        return (result, ctx);
    }

    [Fact]
    public void Trim_Basic_Works()
    {
        var (result, _) = Run("trim \"  hello  \"");
        Assert.Equal("hello", ((Text)result).Content);
    }

    [Fact]
    public void Trim_All_Works()
    {
        var (result, _) = Run("trim/all \" h e l l o \"");
        Assert.Equal("hello", ((Text)result).Content);
    }

    [Fact]
    public void Trim_Lines_Works()
    {
        var (result, _) = Run("trim/lines \"  hello\n  world  \"");
        Assert.Equal("hello world", ((Text)result).Content);
    }

    [Fact]
    public void Trim_Head_Works()
    {
        var (result, _) = Run("trim/head \"  hello  \"");
        Assert.Equal("hello  ", ((Text)result).Content);
    }

    [Fact]
    public void Trim_Tail_Works()
    {
        var (result, _) = Run("trim/tail \"  hello  \"");
        Assert.Equal("  hello", ((Text)result).Content);
    }

    [Fact]
    public void Trim_Head_Tail_Works()
    {
        var (result, _) = Run("trim/head/tail \"  hello  \"");
        Assert.Equal("hello", ((Text)result).Content);
    }

    [Fact]
    public void Replace_First_Works()
    {
        var (result, _) = Run("replace \"banana\" \"a\" \"o\"");
        Assert.Equal("bonana", ((Text)result).Content);
    }

    [Fact]
    public void Replace_All_Works()
    {
        var (result, _) = Run("replace/all \"banana\" \"a\" \"o\"");
        Assert.Equal("bonono", ((Text)result).Content);
    }

    [Fact]
    public void Uppercase_Works()
    {
        var (result, _) = Run("uppercase \"hello\"");
        Assert.Equal("HELLO", ((Text)result).Content);
    }

    [Fact]
    public void Lowercase_Works()
    {
        var (result, _) = Run("lowercase \"HELLO\"");
        Assert.Equal("hello", ((Text)result).Content);
    }

    [Fact]
    public void Split_Works()
    {
        var (result, _) = Run("split \"one,two,three\" \",\"");
        var block = Assert.IsType<Block>(result);
        Assert.Equal(3, block.Children.Count);
        Assert.Equal("one", ((Text)block.Children[0]).Content);
        Assert.Equal("two", ((Text)block.Children[1]).Content);
        Assert.Equal("three", ((Text)block.Children[2]).Content);
    }
}
