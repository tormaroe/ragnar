using Xunit;
using Ragnar;
using System;

namespace Ragnar.Tests;

public class ParseTests : TestBase
{
    [Fact]
    public void Parse_None_Whitespace_Works()
    {
        var (result, _) = Run("parse \"hello world\" none");
        var block = Assert.IsType<Block>(result);
        Assert.Equal(2, block.Children.Count);
        Assert.Equal("hello", ((Text)block.Children[0]).Content);
        Assert.Equal("world", ((Text)block.Children[1]).Content);
    }

    [Fact]
    public void Parse_None_DefaultDelimiters_Works()
    {
        var (result, _) = Run("parse \"here there,everywhere; ok\" none");
        var block = Assert.IsType<Block>(result);
        Assert.Equal(4, block.Children.Count);
        Assert.Equal("here", ((Text)block.Children[0]).Content);
        Assert.Equal("there", ((Text)block.Children[1]).Content);
        Assert.Equal("everywhere", ((Text)block.Children[2]).Content);
        Assert.Equal("ok", ((Text)block.Children[3]).Content);
    }

    [Fact]
    public void Parse_None_DoubleDelimiter_Collapsed()
    {
        var (result, _) = Run("parse \"a,,b\" none");
        var block = Assert.IsType<Block>(result);
        Assert.Equal(2, block.Children.Count);
        Assert.Equal("a", ((Text)block.Children[0]).Content);
        Assert.Equal("b", ((Text)block.Children[1]).Content);
    }

    [Fact]
    public void Parse_CustomDelimiter_Works()
    {
        var (result, _) = Run("parse \"707-467-8000\" \"-\"");
        var block = Assert.IsType<Block>(result);
        Assert.Equal(3, block.Children.Count);
        Assert.Equal("707", ((Text)block.Children[0]).Content);
        Assert.Equal("467", ((Text)block.Children[1]).Content);
        Assert.Equal("8000", ((Text)block.Children[2]).Content);
    }

    [Fact]
    public void Parse_All_PreservesEmptyFields()
    {
        var (result, _) = Run("parse/all \"a,,b\" \",\"");
        var block = Assert.IsType<Block>(result);
        Assert.Equal(3, block.Children.Count);
        Assert.Equal("a", ((Text)block.Children[0]).Content);
        Assert.Equal("", ((Text)block.Children[1]).Content);
        Assert.Equal("b", ((Text)block.Children[2]).Content);
    }

    [Fact]
    public void Parse_All_PreservesBoundaryEmptyFields()
    {
        var (result, _) = Run("parse/all \",a,,b,\" \",\"");
        var block = Assert.IsType<Block>(result);
        Assert.Equal(5, block.Children.Count);
        Assert.Equal("", ((Text)block.Children[0]).Content);
        Assert.Equal("a", ((Text)block.Children[1]).Content);
        Assert.Equal("", ((Text)block.Children[2]).Content);
        Assert.Equal("b", ((Text)block.Children[3]).Content);
        Assert.Equal("", ((Text)block.Children[4]).Content);
    }

    [Fact]
    public void Parse_All_EmptyString_ReturnsEmptyBlock()
    {
        var (result, _) = Run("parse/all \"\" \",\"");
        var block = Assert.IsType<Block>(result);
        Assert.Empty(block.Children);
    }

    [Fact]
    public void Parse_All_SingleDelimiterOnly()
    {
        var (result, _) = Run("parse/all \",\" \",\"");
        var block = Assert.IsType<Block>(result);
        Assert.Equal(2, block.Children.Count);
        Assert.Equal("", ((Text)block.Children[0]).Content);
        Assert.Equal("", ((Text)block.Children[1]).Content);
    }

    [Fact]
    public void Parse_All_NoneRule_NoSplitting()
    {
        var (result, _) = Run("parse/all \"a b\" none");
        var block = Assert.IsType<Block>(result);
        Assert.Single(block.Children);
        Assert.Equal("a b", ((Text)block.Children[0]).Content);
    }

    [Fact]
    public void Parse_CaseInsensitive_Default()
    {
        var (result, _) = Run("parse \"aAb\" \"a\"");
        var block = Assert.IsType<Block>(result);
        Assert.Single(block.Children);
        Assert.Equal("b", ((Text)block.Children[0]).Content);
    }

    [Fact]
    public void Parse_CaseSensitive_WithCaseRefinement()
    {
        var (result, _) = Run("parse/case \"aAb\" \"a\"");
        var block = Assert.IsType<Block>(result);
        Assert.Single(block.Children);
        Assert.Equal("Ab", ((Text)block.Children[0]).Content);
    }

    [Fact]
    public void Parse_AllCase_Works()
    {
        var (result, _) = Run("parse/all/case \"aAb\" \"a\"");
        var block = Assert.IsType<Block>(result);
        Assert.Equal(2, block.Children.Count);
        Assert.Equal("", ((Text)block.Children[0]).Content);
        Assert.Equal("Ab", ((Text)block.Children[1]).Content);
    }

    [Fact]
    public void Parse_InvalidArgs_Throws()
    {
        Assert.ThrowsAny<Exception>(() => Run("parse 123 none"));
        Assert.ThrowsAny<Exception>(() => Run("parse \"abc\" 123"));
    }
}
