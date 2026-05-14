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
}
