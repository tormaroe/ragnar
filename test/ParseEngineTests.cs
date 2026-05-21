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

    [Fact]
    public void Parse_Charset_Basic_Works()
    {
        // A charset of digits should match one digit character
        var (res1, _) = Run("digits: charset \"0123456789\" parse \"5\" [digits]");
        Assert.True(((Logic)res1).Condition);

        var (res2, _) = Run("digits: charset \"0123456789\" parse \"a\" [digits]");
        Assert.False(((Logic)res2).Condition);

        // Three digits
        var (res3, _) = Run("digits: charset \"0123456789\" parse \"123\" [3 digits]");
        Assert.True(((Logic)res3).Condition);
    }

    [Fact]
    public void Parse_PhoneNumber_RebolExample_Works()
    {
        // Rebol documentation example:
        // digits: charset "0123456789"
        // area-code: ["(" 3 digits ")"]
        // phone-num: [3 digits "-" 4 digits]
        // parse "(707)467-8000" [[area-code | none] phone-num]  => true
        var code = """
            digits: charset "0123456789"
            area-code: ["(" 3 digits ")"]
            phone-num: [3 digits "-" 4 digits]
            parse "(707)467-8000" [[area-code | none] phone-num]
            """;
        var (res, _) = Run(code);
        Assert.True(((Logic)res).Condition);
    }

    [Fact]
    public void Parse_PhoneNumber_WithoutAreaCode_Works()
    {
        // The "none" alternative should allow matching just the phone-num part
        var code = """
            digits: charset "0123456789"
            area-code: ["(" 3 digits ")"]
            phone-num: [3 digits "-" 4 digits]
            parse "467-8000" [[area-code | none] phone-num]
            """;
        var (res, _) = Run(code);
        Assert.True(((Logic)res).Condition);
    }

    // ── not / ahead ──────────────────────────────────────────────────────────

    [Fact]
    public void Parse_Not_Lookahead_Works()
    {
        // not fails if the pattern matches (negative lookahead)
        var (res1, _) = Run("parse \"ab\" [not #\"x\" #\"a\" #\"b\"]");
        Assert.True(((Logic)res1).Condition);

        var (res2, _) = Run("parse \"ab\" [not #\"a\" #\"a\" #\"b\"]");
        Assert.False(((Logic)res2).Condition);
    }

    [Fact]
    public void Parse_Ahead_Lookahead_Works()
    {
        // ahead succeeds if pattern matches, but doesn't consume
        var (res1, _) = Run("parse \"ab\" [ahead #\"a\" #\"a\" #\"b\"]");
        Assert.True(((Logic)res1).Condition);

        var (res2, _) = Run("parse \"ab\" [ahead #\"x\" #\"a\" #\"b\"]");
        Assert.False(((Logic)res2).Condition);
    }

    // ── fail ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_Fail_ForcesFailure()
    {
        // fail always fails the current alternative
        var (res1, _) = Run("parse \"a\" [fail]");
        Assert.False(((Logic)res1).Condition);

        // fail triggers backtracking to next alternative
        var (res2, _) = Run("parse \"a\" [fail | #\"a\"]");
        Assert.True(((Logic)res2).Condition);
    }

    // ── break / reject ────────────────────────────────────────────────────────

    [Fact]
    public void Parse_Break_ExitsLoopWithSuccess()
    {
        // break exits any/some loop with success immediately
        var (res1, _) = Run("parse \"aaa\" [any [#\"a\" | break]]");
        Assert.True(((Logic)res1).Condition);

        // break inside some exits even before minimum met
        var (res2, _) = Run("parse \"\" [some [break]]");
        Assert.True(((Logic)res2).Condition);
    }

    [Fact]
    public void Parse_Reject_ExitsLoopWithFailure()
    {
        // reject exits any/some loop with failure
        var (res1, _) = Run("parse \"a\" [any [reject] | #\"a\"]");
        Assert.True(((Logic)res1).Condition);
    }

    // ── if ────────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_If_ConditionalRule_Works()
    {
        // if with true paren passes
        var (res1, _) = Run("parse \"a\" [if (true) #\"a\"]");
        Assert.True(((Logic)res1).Condition);

        // if with false paren fails
        var (res2, _) = Run("parse \"a\" [if (false) #\"a\"]");
        Assert.False(((Logic)res2).Condition);

        // if can test a variable
        var (res3, _) = Run("x: true  parse \"a\" [if (x) #\"a\"]");
        Assert.True(((Logic)res3).Condition);
    }

    // ── while ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_While_Works()
    {
        // while matches zero or more times (like any, but no no-progress guard)
        var (res1, _) = Run("parse \"aaa\" [while #\"a\"]");
        Assert.True(((Logic)res1).Condition);

        var (res2, _) = Run("parse \"\" [while #\"a\"]");
        Assert.True(((Logic)res2).Condition);

        var (res3, _) = Run("digits: charset \"0123456789\" parse \"123abc\" [while digits \"abc\"]");
        Assert.True(((Logic)res3).Condition);
    }

    // ── then ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_Then_CommitsToCurrentBranch()
    {
        // Without then, "b" alternative would be tried
        // With then after "a", "b" alternative is skipped even when "a" branch fails overall
        var (res1, _) = Run("parse \"a\" [#\"a\" then | #\"b\"]");
        Assert.True(((Logic)res1).Condition);

        // When then branch itself succeeds, parse should succeed
        var (res2, _) = Run("parse \"ab\" [#\"a\" then #\"b\"]");
        Assert.True(((Logic)res2).Condition);
    }

    // ── into ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_Into_SubParseBlock_Works()
    {
        // into: parse a nested block within the outer block
        var (res1, _) = Run("parse [[1 2 3]] [into [1 2 3]]");
        Assert.True(((Logic)res1).Condition);

        var (res2, _) = Run("parse [[1 2]] [into [1 2 3]]");
        Assert.False(((Logic)res2).Condition);

        // into with a string nested in a block
        var (res3, _) = Run("parse [\"hello\"] [into [\"hello\"]]");
        Assert.True(((Logic)res3).Condition);
    }

    // ── quote ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_Quote_LiteralMatch_Works()
    {
        // quote matches a word literally without treating it as a variable reference
        var (res1, _) = Run("parse [hello] [quote hello]");
        Assert.True(((Logic)res1).Condition);

        var (res2, _) = Run("parse [world] [quote hello]");
        Assert.False(((Logic)res2).Condition);
    }

    // ── insert / remove / change ──────────────────────────────────────────────

    [Fact]
    public void Parse_Insert_String_Works()
    {
        // insert adds text at current position
        var (_, ctx) = Run("""
            s: copy "hello"
            parse s [insert "X"]
            s
            """);
        Assert.Equal("Xhello", ((Text)ctx.Get("s")).Content);
    }

    [Fact]
    public void Parse_Remove_String_Works()
    {
        // remove deletes the matched portion
        var (_, ctx) = Run("""
            s: copy "hello world"
            parse s [thru " " remove "world"]
            s
            """);
        Assert.Equal("hello ", ((Text)ctx.Get("s")).Content);
    }

    [Fact]
    public void Parse_Remove_Block_Works()
    {
        // remove from a block
        var (res, ctx) = Run("""
            b: copy [1 2 3 4]
            parse b [skip remove integer!]
            b
            """);
        var block = Assert.IsType<Block>(ctx.Get("b"));
        Assert.Equal(3, block.Children.Count);
        Assert.Equal(1L, ((Integer)block.Children[0]).Number);
        Assert.Equal(3L, ((Integer)block.Children[1]).Number);
    }

    [Fact]
    public void Parse_Change_String_Works()
    {
        // change replaces matched content with new value
        var (_, ctx) = Run("""
            s: copy "hello world"
            parse s [change "hello" "goodbye"]
            s
            """);
        Assert.Equal("goodbye world", ((Text)ctx.Get("s")).Content);
    }

    [Fact]
    public void Parse_Change_Block_Works()
    {
        // change replaces an element in a block
        var (_, ctx) = Run("""
            b: copy [1 2 3]
            parse b [skip change integer! 99]
            b
            """);
        var block = Assert.IsType<Block>(ctx.Get("b"));
        Assert.Equal(3, block.Children.Count);
        Assert.Equal(1L, ((Integer)block.Children[0]).Number);
        Assert.Equal(99L, ((Integer)block.Children[1]).Number);
        Assert.Equal(3L, ((Integer)block.Children[2]).Number);
    }
}
