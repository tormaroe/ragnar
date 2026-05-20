using Xunit;
using Ragnar;
using System;

namespace Ragnar.Tests;

public class ParseEngineTests : TestBase
{
    [Fact]
    public void Parse_Literals_Works()
    {
        var (res1, _) = Run("parse \"a\" [#\"a\"]");
        Assert.True(((Logic)res1).Condition);

        var (res2, _) = Run("parse \"ab\" [#\"a\" #\"b\"]");
        Assert.True(((Logic)res2).Condition);

        var (res3, _) = Run("parse \"abc\" [\"abc\"]");
        Assert.True(((Logic)res3).Condition);

        var (res4, _) = Run("parse \"abc\" [\"ab\"]");
        Assert.False(((Logic)res4).Condition);
    }

    [Fact]
    public void Parse_Alternatives_Works()
    {
        var (res1, _) = Run("parse \"a\" [#\"a\" | #\"b\"]");
        Assert.True(((Logic)res1).Condition);

        var (res2, _) = Run("parse \"b\" [#\"a\" | #\"b\"]");
        Assert.True(((Logic)res2).Condition);

        var (res3, _) = Run("parse \"c\" [#\"a\" | #\"b\"]");
        Assert.False(((Logic)res3).Condition);

        var (res4, _) = Run("parse \"abc\" [#\"a\" #\"b\" #\"c\" | #\"a\" #\"x\"]");
        Assert.True(((Logic)res4).Condition);
    }

    [Fact]
    public void Parse_Repetition_Any_Works()
    {
        var (res1, _) = Run("parse \"aaa\" [any #\"a\"]");
        Assert.True(((Logic)res1).Condition);

        var (res2, _) = Run("parse \"\" [any #\"a\"]");
        Assert.True(((Logic)res2).Condition);

        var (res3, _) = Run("parse \"ab\" [any #\"a\" #\"b\"]");
        Assert.True(((Logic)res3).Condition);
    }

    [Fact]
    public void Parse_Repetition_Some_Works()
    {
        var (res1, _) = Run("parse \"aaa\" [some #\"a\"]");
        Assert.True(((Logic)res1).Condition);

        var (res2, _) = Run("parse \"\" [some #\"a\"]");
        Assert.False(((Logic)res2).Condition);
    }

    [Fact]
    public void Parse_Repetition_Opt_Works()
    {
        var (res1, _) = Run("parse \"a\" [opt #\"a\"]");
        Assert.True(((Logic)res1).Condition);

        var (res2, _) = Run("parse \"\" [opt #\"a\"]");
        Assert.True(((Logic)res2).Condition);

        var (res3, _) = Run("parse \"b\" [opt #\"a\" #\"b\"]");
        Assert.True(((Logic)res3).Condition);
    }

    [Fact]
    public void Parse_NumericCount_Works()
    {
        var (res1, _) = Run("parse \"aaa\" [3 #\"a\"]");
        Assert.True(((Logic)res1).Condition);

        var (res2, _) = Run("parse \"aa\" [3 #\"a\"]");
        Assert.False(((Logic)res2).Condition);

        var (res3, _) = Run("parse \"aaaa\" [3 #\"a\"]");
        Assert.False(((Logic)res3).Condition);
    }

    [Fact]
    public void Parse_NumericRange_Works()
    {
        var (res1, _) = Run("parse \"aa\" [1 3 #\"a\"]");
        Assert.True(((Logic)res1).Condition);

        var (res2, _) = Run("parse \"aaaa\" [1 3 #\"a\"]");
        Assert.False(((Logic)res2).Condition);
    }

    [Fact]
    public void Parse_SubRules_Works()
    {
        var (res, _) = Run("digit: [#\"0\" | #\"1\" | #\"2\"] parse \"021\" [digit digit digit]");
        Assert.True(((Logic)res).Condition);

        var (res2, _) = Run("digit: [#\"0\" | #\"1\" | #\"2\"] parse \"031\" [digit digit digit]");
        Assert.False(((Logic)res2).Condition);
    }

    [Fact]
    public void Parse_Backtracking_Repetition_Works()
    {
        var (res, _) = Run("parse \"aa\" [any #\"a\" #\"a\"]");
        Assert.True(((Logic)res).Condition);
    }

    [Fact]
    public void Parse_CaseSensitivity_Works()
    {
        var (res1, _) = Run("parse \"AbC\" [#\"a\" \"bc\"]");
        Assert.True(((Logic)res1).Condition);

        var (res2, _) = Run("parse/case \"AbC\" [#\"a\" \"bc\"]");
        Assert.False(((Logic)res2).Condition);

        var (res3, _) = Run("parse/case \"AbC\" [#\"A\" \"bC\"]");
        Assert.True(((Logic)res3).Condition);
    }

    [Fact]
    public void Parse_Skip_Works()
    {
        var (res1, _) = Run("parse \"abc\" [skip \"bc\"]");
        Assert.True(((Logic)res1).Condition);

        var (res2, _) = Run("parse \"abc\" [skip skip skip]");
        Assert.True(((Logic)res2).Condition);

        var (res3, _) = Run("parse \"abc\" [skip skip skip skip]");
        Assert.False(((Logic)res3).Condition);
    }

    [Fact]
    public void Parse_End_Works()
    {
        var (res1, _) = Run("parse \"abc\" [\"abc\" end]");
        Assert.True(((Logic)res1).Condition);

        var (res2, _) = Run("parse \"abc\" [\"ab\" end]");
        Assert.False(((Logic)res2).Condition);
    }

    [Fact]
    public void Parse_To_And_Thru_Works()
    {
        var (res1, _) = Run("parse \"a b c\" [to \"c\" \"c\"]");
        Assert.True(((Logic)res1).Condition);

        var (res2, _) = Run("parse \"a b c\" [thru \"b\" \" c\"]");
        Assert.True(((Logic)res2).Condition);

        var (res3, _) = Run("parse \"a b a c\" [to \"a\" \"a c\"]");
        Assert.True(((Logic)res3).Condition);

        var (res4, _) = Run("parse \"a b c\" [to end]");
        Assert.True(((Logic)res4).Condition);
    }

    [Fact]
    public void Parse_ParenExecution_Works()
    {
        var (res, ctx) = Run("x: 0 parse \"a\" [(x: 1) #\"a\"]");
        Assert.True(((Logic)res).Condition);
        Assert.Equal(1, ((Integer)ctx.Get("x")).Number);
    }

    [Fact]
    public void Parse_PositionMarkers_Works()
    {
        var (res, _) = Run("parse \"a b c\" [to \"b\" p: \"b\" to \"c\" :p \"b c\"]");
        Assert.True(((Logic)res).Condition);
    }

    [Fact]
    public void Parse_Copy_Works()
    {
        var (res, ctx) = Run("parse \"abc\" [copy val \"ab\" \"c\"]");
        Assert.True(((Logic)res).Condition);
        var val = ctx.Get("val");
        Assert.Equal("ab", ((Text)val).Content);
    }

    [Fact]
    public void Parse_Set_Works()
    {
        var (res, ctx) = Run("parse \"abc\" [set val #\"a\" \"bc\"]");
        Assert.True(((Logic)res).Condition);
        var val = ctx.Get("val");
        var c = Assert.IsType<Character>(val);
        Assert.Equal('a', c.CharValue);
    }

    [Fact]
    public void Parse_Copy_Repetition_Works()
    {
        var (res, ctx) = Run("parse \"aaa\" [copy val some #\"a\"]");
        Assert.True(((Logic)res).Condition);
        var val = ctx.Get("val");
        Assert.Equal("aaa", ((Text)val).Content);
    }

    [Fact]
    public void Parse_Block_Literals_Works()
    {
        var (res1, _) = Run("parse [1 2 3] [1 2 3]");
        Assert.True(((Logic)res1).Condition);

        var (res2, _) = Run("parse [1 2 3] [1 2]");
        Assert.False(((Logic)res2).Condition);

        var (res3, _) = Run("parse [\"hello\" #\"a\"] [\"hello\" #\"a\"]");
        Assert.True(((Logic)res3).Condition);
    }

    [Fact]
    public void Parse_Block_LitWords_Works()
    {
        var (res, _) = Run("parse [hello world] ['hello 'world]");
        Assert.True(((Logic)res).Condition);
    }

    [Fact]
    public void Parse_Block_Datatypes_Works()
    {
        var (res1, _) = Run("parse [123 \"hello\" #\"x\"] [integer! string! char!]");
        Assert.True(((Logic)res1).Condition);

        var (res2, _) = Run("parse [123 [a b] hello] [integer! block! word!]");
        Assert.True(((Logic)res2).Condition);
    }

    [Fact]
    public void Parse_Block_Repetition_Works()
    {
        var (res, _) = Run("parse [1 2 3] [some integer!]");
        Assert.True(((Logic)res).Condition);

        var (res2, _) = Run("parse [1 \"two\" 3] [some [integer! | string!]]");
        Assert.True(((Logic)res2).Condition);
    }

    [Fact]
    public void Parse_Block_To_Thru_Works()
    {
        var (res, _) = Run("parse [1 2 hello 3] [to 'hello 'hello 3]");
        Assert.True(((Logic)res).Condition);
    }

    [Fact]
    public void Parse_Block_CopySet_Works()
    {
        var (res1, ctx1) = Run("parse [1 2 3] [copy val some integer!]");
        Assert.True(((Logic)res1).Condition);
        var val1 = ctx1.Get("val");
        var b = Assert.IsType<Block>(val1);
        Assert.Equal(3, b.Children.Count);
        Assert.Equal(1, ((Integer)b.Children[0]).Number);
        Assert.Equal(2, ((Integer)b.Children[1]).Number);
        Assert.Equal(3, ((Integer)b.Children[2]).Number);

        var (res2, ctx2) = Run("parse [1 2 3] [set val integer! to end]");
        Assert.True(((Logic)res2).Condition);
        var val2 = ctx2.Get("val");
        Assert.Equal(1, ((Integer)val2).Number);
    }

    [Fact]
    public void Parse_Block_PositionMarkers_Works()
    {
        var (res, _) = Run("parse [1 2 3] [integer! p: integer! :p integer! integer!]");
        Assert.True(((Logic)res).Condition);
    }
}
