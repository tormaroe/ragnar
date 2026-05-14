using Xunit;
using Ragnar;
using System;

namespace Ragnar.Tests;

public class SeriesTests
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
    public void Find_In_Text_Returns_Tail()
    {
        var (result, _) = Run("find \"abcdef\" \"cd\"");
        Assert.IsType<Text>(result);
        Assert.Equal("cdef", ((Text)result).ToUserString());
    }

    [Fact]
    public void Find_In_Block_Returns_Tail()
    {
        var (result, _) = Run("find [a b c d] 'c");
        Assert.IsType<Block>(result);
        Assert.Equal("[ c d ]", result.ToString());
    }

    [Fact]
    public void Find_Returns_None_If_Not_Found()
    {
        var (resText, _) = Run("find \"abc\" \"z\"");
        Assert.Equal("none", ((Word)resText).Name);

        var (resBlock, _) = Run("find [a b c] 'z");
        Assert.Equal("none", ((Word)resBlock).Name);
    }

    [Fact]
    public void Series_Functions_Respect_Index()
    {
        var code = @"
            s: find ""abcdef"" ""cd""
            l: length? s
            f: first s
            snd: second s
        ";
        var (_, ctx) = Run(code);

        Assert.Equal(4, ((Integer)ctx.Get("l")).Number);
        Assert.Equal("c", ((Text)ctx.Get("f")).ToUserString());
        Assert.Equal("d", ((Text)ctx.Get("snd")).ToUserString());
    }

    [Fact]
    public void Block_Functions_Respect_Index()
    {
        var code = @"
            b: find [10 20 30 40] 30
            l: length? b
            f: first b
            snd: second b
        ";
        var (_, ctx) = Run(code);

        Assert.Equal(2, ((Integer)ctx.Get("l")).Number);
        Assert.Equal(30, ((Integer)ctx.Get("f")).Number);
        Assert.Equal(40, ((Integer)ctx.Get("snd")).Number);
    }

    [Fact]
    public void Last_Respects_Index()
    {
        var (resText, _) = Run("last find \"abcdef\" \"cd\"");
        Assert.Equal("f", ((Text)resText).ToUserString());

        var (resBlock, _) = Run("last find [10 20 30 40] 20");
        Assert.Equal(40, ((Integer)resBlock).Number);
    }
}
