using Xunit;
using Ragnar;
using System;

namespace Ragnar.Tests;

public class CharTests : TestBase
{
    [Fact]
    public void CharLiteral_Basic_Works()
    {
        var (result, _) = Run("#\"a\"");
        var c = Assert.IsType<Character>(result);
        Assert.Equal('a', c.CharValue);
        Assert.Equal("#\"a\"", c.ToString());
    }

    [Fact]
    public void CharLiteral_Escaped_Works()
    {
        var (resLf, _) = Run("#\"^/\"");
        var lf = Assert.IsType<Character>(resLf);
        Assert.Equal('\n', lf.CharValue);
        Assert.Equal("#\"^/\"", lf.ToString());

        var (resTab, _) = Run("#\"^-\"");
        var tab = Assert.IsType<Character>(resTab);
        Assert.Equal('\t', tab.CharValue);
        Assert.Equal("#\"^-\"", tab.ToString());

        var (resCaret, _) = Run("#\"^^\"");
        var caret = Assert.IsType<Character>(resCaret);
        Assert.Equal('^', caret.CharValue);
        Assert.Equal("#\"^^\"", caret.ToString());

        var (resQuote, _) = Run("#\"^\"\"");
        var quote = Assert.IsType<Character>(resQuote);
        Assert.Equal('"', quote.CharValue);
        Assert.Equal("#\"^\"\"", quote.ToString());
    }

    [Fact]
    public void CharPredicate_Works()
    {
        var (res1, _) = Run("char? #\"x\"");
        Assert.True(((Logic)res1).Condition);

        var (res2, _) = Run("char? \"x\"");
        Assert.False(((Logic)res2).Condition);

        var (res3, _) = Run("char? 123");
        Assert.False(((Logic)res3).Condition);
    }

    [Fact]
    public void TypeQuestion_ReturnsCharType()
    {
        var (result, _) = Run("type? #\"a\"");
        var w = Assert.IsType<Word>(result);
        Assert.Equal("char!", w.Name);
    }

    [Fact]
    public void ToChar_FromChar_ReturnsSelf()
    {
        var (result, _) = Run("to-char #\"z\"");
        var c = Assert.IsType<Character>(result);
        Assert.Equal('z', c.CharValue);
    }

    [Fact]
    public void ToChar_FromInteger_Works()
    {
        var (result, _) = Run("to-char 65");
        var c = Assert.IsType<Character>(result);
        Assert.Equal('A', c.CharValue);
    }

    [Fact]
    public void ToChar_FromString_Works()
    {
        var (result, _) = Run("to-char \"Hello\"");
        var c = Assert.IsType<Character>(result);
        Assert.Equal('H', c.CharValue);
    }

    [Fact]
    public void ToChar_FromStringWithIndexOffset_Works()
    {
        var (result, _) = Run("to-char next \"Hello\"");
        var c = Assert.IsType<Character>(result);
        Assert.Equal('e', c.CharValue);
    }

    [Fact]
    public void ToChar_Invalid_Throws()
    {
        Assert.ThrowsAny<Exception>(() => Run("to-char \"\""));
        Assert.ThrowsAny<Exception>(() => Run("to-char 9999999"));
        Assert.ThrowsAny<Exception>(() => Run("to-char [hello]"));
    }
}
