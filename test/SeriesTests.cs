using Xunit;
using Ragnar;
using System;

namespace Ragnar.Tests;

public class SeriesTests : TestBase
{
    [Fact]
    public void Pick_And_Poke_Work()
    {
        var code = @"
            b: [10 20 30]
            p1: pick b 1
            poke b 2 99
            p2: pick b 2
        ";
        var (_, ctx) = Run(code);
        Assert.Equal(10, ((Integer)ctx.Get("p1")).Number);
        Assert.Equal(99, ((Integer)ctx.Get("p2")).Number);
    }

    [Fact]
    public void Index_Works()
    {
        var code = @"
            s: ""abcdef""
            s2: find s ""cd""
            idx: index? s2
        ";
        var (_, ctx) = Run(code);
        Assert.Equal(3, ((Integer)ctx.Get("idx")).Number);
    }

    [Fact]
    public void Copy_Works()
    {
        var code = @"
            b1: [1 2 3]
            b2: copy b1
            append b1 4
        ";
        var (_, ctx) = Run(code);
        var b1 = (Block)ctx.Get("b1");
        var b2 = (Block)ctx.Get("b2");
        Assert.Equal(4, b1.Children.Count);
        Assert.Equal(3, b2.Children.Count);
    }

    [Fact]
    public void Append_Text_Works()
    {
        var code = @"
            t: ""hello""
            append t "" world""
        ";
        var (result, _) = Run(code);
        Assert.Equal("hello world", ((Text)result).ToUserString());
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

    [Fact]
    public void File_Join_Works()
    {
        var code = @"
            dir: %/c/my-folder/
            file: %data.txt
            full-path: join dir file
        ";
        var (_, ctx) = Run(code);
        var res = ctx.Get("full-path");
        Assert.IsType<File>(res);
        Assert.Equal("%/c/my-folder/data.txt", res.ToString());
    }

    [Fact]
    public void File_Rejoin_Works()
    {
        var code = @"
            dir: %/c/my-folder/
            file: %data.txt
            full-path: rejoin [dir ""subfolder/"" file]
        ";
        var (_, ctx) = Run(code);
        var res = ctx.Get("full-path");
        Assert.IsType<File>(res);
        Assert.Equal("%/c/my-folder/subfolder/data.txt", res.ToString());
    }

    [Fact]
    public void File_Join_Adds_Separator()
    {
        var code = @"
            dir: %/c/my-folder
            file: %data.txt
            full-path: join dir file
        ";
        var (_, ctx) = Run(code);
        var res = ctx.Get("full-path");
        Assert.Equal("%/c/my-folder/data.txt", res.ToString());
    }

    [Fact]
    public void Sort_Works()
    {
        var (resText, _) = Run("sort \"cba\"");
        Assert.Equal("abc", ((Text)resText).ToUserString());

        var (resBlock, _) = Run("sort [3 1 2]");
        Assert.Equal("[ 1 2 3 ]", resBlock.ToString());
    }

    [Fact]
    public void Reverse_Works()
    {
        var (resText, _) = Run("reverse \"abc\"");
        Assert.Equal("cba", ((Text)resText).ToUserString());

        var (resBlock, _) = Run("reverse [1 2 3]");
        Assert.Equal("[ 3 2 1 ]", resBlock.ToString());
    }
}
