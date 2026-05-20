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
        // Register digit in context first
        var (res, _) = Run("digit: [#\"0\" | #\"1\" | #\"2\"] parse \"021\" [digit digit digit]");
        Assert.True(((Logic)res).Condition);

        var (res2, _) = Run("digit: [#\"0\" | #\"1\" | #\"2\"] parse \"031\" [digit digit digit]");
        Assert.False(((Logic)res2).Condition);
    }

    [Fact]
    public void Parse_Backtracking_Repetition_Works()
    {
        // any #"a" will match "aa" but backtracking must drop one "a" to let the second rule match
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
}
