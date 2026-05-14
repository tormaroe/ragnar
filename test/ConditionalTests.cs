using Xunit;
using Ragnar;
using System;

namespace Ragnar.Tests;

public class ConditionalTests : TestBase
{
    [Fact]
    public void If_Works()
    {
        var (res1, _) = Run("if true [ 10 ]");
        Assert.Equal(10, ((Integer)res1).Number);

        var (res2, _) = Run("if false [ 10 ]");
        Assert.Equal("none", ((Word)res2).Name);
    }

    [Fact]
    public void Either_Works()
    {
        var (res1, _) = Run("either true [ 1 ] [ 2 ]");
        Assert.Equal(1, ((Integer)res1).Number);

        var (res2, _) = Run("either false [ 1 ] [ 2 ]");
        Assert.Equal(2, ((Integer)res2).Number);
    }

    [Fact]
    public void All_Works()
    {
        var (res1, _) = Run("all [ true 10 \"hi\" ]");
        Assert.Equal("hi", ((Text)res1).ToUserString());

        var (res2, _) = Run("all [ true false 10 ]");
        Assert.Equal("none", ((Word)res2).Name);
    }

    [Fact]
    public void Any_Works()
    {
        var (res1, _) = Run("any [ false none 10 false ]");
        Assert.Equal(10, ((Integer)res1).Number);

        var (res2, _) = Run("any [ false none ]");
        Assert.Equal("none", ((Word)res2).Name);
    }

    [Fact]
    public void Truthiness_Works()
    {
        var (res1, _) = Run("if 10 [ true ]");
        Assert.True(((Logic)res1).Condition);

        var (res2, _) = Run("either none [ 1 ] [ 2 ]");
        Assert.Equal(2, ((Integer)res2).Number);
    }

    [Fact]
    public void Case_Basic_Works()
    {
        var code = @"
            case [
                false [ 1 ]
                true  [ 2 ]
                true  [ 3 ]
            ]
        ";
        var (result, _) = Run(code);
        Assert.Equal(2, ((Integer)result).Number);
    }

    [Fact]
    public void Case_All_Works()
    {
        var code = @"
            a: 0
            case/all [
                true [ a: add a 1 ]
                false [ a: add a 10 ]
                true [ a: add a 100 ]
            ]
            a
        ";
        var (result, _) = Run(code);
        Assert.Equal(101, ((Integer)result).Number);
    }

    [Fact]
    public void Case_Returns_None_If_No_Match()
    {
        var (result, _) = Run("case [ false [ 1 ] ]");
        Assert.Equal("none", ((Word)result).Name);
    }

    [Fact]
    public void Case_Evaluates_Conditions()
    {
        var code = @"
            x: 10
            case [
                less? x 5 [ ""small"" ]
                greater? x 5 [ ""big"" ]
            ]
        ";
        var (result, _) = Run(code);
        Assert.Equal("big", ((Text)result).ToUserString());
    }
}
