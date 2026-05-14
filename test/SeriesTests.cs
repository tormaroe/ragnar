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

    [Fact]
    public void Find_Case_Works()
    {
        var (res1, _) = Run("find \"ABC\" \"a\"");
        Assert.Equal("ABC", ((Text)res1).ToUserString());

        var (res2, _) = Run("find/case \"ABC\" \"a\"");
        Assert.Equal("none", ((Word)res2).Name);
    }

    [Fact]
    public void Find_Last_Works()
    {
        var (resText, _) = Run("find/last \"banana\" \"a\"");
        Assert.Equal(1, ((Text)resText).Length); // Just the last "a"
        Assert.Equal("a", ((Text)resText).ToUserString());

        var (resBlock, _) = Run("find/last [a b a c] 'a");
        Assert.Equal("[ a c ]", resBlock.ToString());
    }

    [Fact]
    public void Find_Tail_Works()
    {
        var (resText, _) = Run("find/tail \"abcdef\" \"cd\"");
        Assert.Equal("ef", ((Text)resText).ToUserString());

        var (resBlock, _) = Run("find/tail [10 20 30 40] 30");
        Assert.Equal("[ 40 ]", resBlock.ToString());
    }

    [Fact]
    public void Find_Match_Works()
    {
        var (res1, _) = Run("find/match \"abcdef\" \"abc\"");
        Assert.Equal("abcdef", ((Text)res1).ToUserString());

        var (res2, _) = Run("find/match \"abcdef\" \"bcd\"");
        Assert.Equal("none", ((Word)res2).Name);
    }

    [Fact]
    public void Find_Any_Works()
    {
        var (res1, _) = Run("find/any \"abcdef\" \"a?c\"");
        Assert.Equal("abcdef", ((Text)res1).ToUserString());

        var (res2, _) = Run("find/any \"abcdef\" \"a*e\"");
        Assert.Equal("abcdef", ((Text)res2).ToUserString());
    }
}
