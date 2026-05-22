using Xunit;
using Ragnar;
using System.Collections.Generic;

namespace Ragnar.Tests;

public class FormatterTests : TestBase
{
    [Fact]
    public void ShortListLiteral_StaysOnSingleLine()
    {
        var (result, ctx) = Run("format [1 2 3 4 5]");
        Assert.Equal("[ 1 2 3 4 5 ]", ((Text)result).Content);
    }

    [Fact]
    public void ShortListLiteralWithStrings_StaysOnSingleLine()
    {
        var (result, ctx) = Run("format [hello world]");
        Assert.Equal("[ hello world ]", ((Text)result).Content);
    }

    [Fact]
    public void LongListLiteral_FormatsMultiLine()
    {
        var (result, ctx) = Run("format [1 2 3 4 5 6 7]");
        var expected = "[\n    1\n    2\n    3\n    4\n    5\n    6\n    7\n]";
        Assert.Equal(expected, ((Text)result).Content.Replace("\r\n", "\n"));
    }

    [Fact]
    public void NestedListLiteral_FormatsMultiLine()
    {
        var (result, ctx) = Run("format [1 [2] 3]");
        var expected = "[\n    1\n    [ 2 ]\n    3\n]";
        Assert.Equal(expected, ((Text)result).Content.Replace("\r\n", "\n"));
    }

    [Fact]
    public void MakeObject_FormatsIdiomatically()
    {
        var (result, ctx) = Run("format {make object! [ a: 1 b: 2 ]}");
        var expected = "make object! [\n    a: 1\n    b: 2\n]";
        Assert.Equal(expected, ((Text)result).Content.Replace("\r\n", "\n"));
    }

    [Fact]
    public void Func_FormatsIdiomatically()
    {
        var (result, ctx) = Run("format {func [x y] [x + y]}");
        var expected = "func [ x y ] [\n    x + y\n]";
        Assert.Equal(expected, ((Text)result).Content.Replace("\r\n", "\n"));
    }

    [Fact]
    public void Does_FormatsIdiomatically()
    {
        var (result, ctx) = Run("format {does [print \"hello\"]}");
        var expected = "does [\n    print \"hello\"\n]";
        Assert.Equal(expected, ((Text)result).Content.Replace("\r\n", "\n"));
    }

    [Fact]
    public void Either_FormatsIdiomatically()
    {
        var (result, ctx) = Run("format {either x > 10 [print \"yes\"] [print \"no\"]}");
        var expected = "either x > 10 [\n    print \"yes\"\n] [\n    print \"no\"\n]";
        Assert.Equal(expected, ((Text)result).Content.Replace("\r\n", "\n"));
    }

    [Fact]
    public void If_FormatsIdiomatically()
    {
        var (result, ctx) = Run("format {if x > 10 [print \"yes\"]}");
        var expected = "if x > 10 [\n    print \"yes\"\n]";
        Assert.Equal(expected, ((Text)result).Content.Replace("\r\n", "\n"));
    }

    [Fact]
    public void Foreach_FormatsIdiomatically()
    {
        var (result, ctx) = Run("format {foreach item items [print item]}");
        var expected = "foreach item items [\n    print item\n]";
        Assert.Equal(expected, ((Text)result).Content.Replace("\r\n", "\n"));
    }

    [Fact]
    public void Switch_FormatsIdiomatically()
    {
        var (result, ctx) = Run("format {switch val [1 [print \"one\"] 2 [print \"two\"]]}");
        var expected = "switch val [\n    1 [\n        print \"one\"\n    ]\n    2 [\n        print \"two\"\n    ]\n]";
        Assert.Equal(expected, ((Text)result).Content.Replace("\r\n", "\n"));
    }

    [Fact]
    public void SwitchDefault_FormatsIdiomatically()
    {
        var (result, ctx) = Run("format {switch/default val [1 [print \"one\"]] [print \"default\"]}");
        var expected = "switch/default val [\n    1 [\n        print \"one\"\n    ]\n] [\n    print \"default\"\n]";
        Assert.Equal(expected, ((Text)result).Content.Replace("\r\n", "\n"));
    }

    [Fact]
    public void Format_BlockValueDirectly()
    {
        var (result, ctx) = Run("format [a: 1 b: 2 c: 3 d: 4 e: 5 f: 6 g: 7]");
        var expected = "[\n    a: 1\n    b: 2\n    c: 3\n    d: 4\n    e: 5\n    f: 6\n    g: 7\n]";
        Assert.Equal(expected, ((Text)result).Content.Replace("\r\n", "\n"));
    }

    [Fact]
    public void Help_DisplaysFormattedBody()
    {
        var code = @"
            my-func: func [a b] [
                either a > b [
                    a
                ] [
                    b
                ]
            ]
            help my-func
        ";
        var (result, output) = RunWithOutput(code);
        
        var expectedBodyPart = "BODY:  [\n    either a > b [\n        a\n    ] [\n        b\n    ]\n]";
        Assert.Contains(expectedBodyPart.Replace("\r\n", "\n"), output.Replace("\r\n", "\n"));
    }

    [Fact]
    public void FormatScript_WithBlock_FormatsChildrenWithoutBrackets()
    {
        var (result, ctx) = Run("format/script [a: 1 b: 2]");
        var expected = "a: 1\nb: 2";
        Assert.Equal(expected, ((Text)result).Content.Replace("\r\n", "\n"));
    }

    [Fact]
    public void FormatScript_WithNestedBlock_FormatsChildrenWithoutBrackets()
    {
        var (result, ctx) = Run("format/script [if x > 10 [print \"yes\"]]");
        var expected = "if x > 10 [\n    print \"yes\"\n]";
        Assert.Equal(expected, ((Text)result).Content.Replace("\r\n", "\n"));
    }

    [Fact]
    public void FormatBlock_WithoutScriptRefinement_FormatsWithBrackets()
    {
        var (result, ctx) = Run("format [a: 1 b: 2]");
        var expected = "[ a: 1 b: 2 ]";
        Assert.Equal(expected, ((Text)result).Content.Replace("\r\n", "\n"));
    }
}
