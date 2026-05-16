using Xunit;
using Ragnar;
using System;

namespace Ragnar.Tests;

public class LogicalTests : TestBase
{
    [Fact]
    public void Not_Works()
    {
        var (res1, _) = Run("not true");
        Assert.False(((Logic)res1).Condition);

        var (res2, _) = Run("not false");
        Assert.True(((Logic)res2).Condition);

        var (res3, _) = Run("not none");
        Assert.True(((Logic)res3).Condition);

        var (res4, _) = Run("not 10");
        Assert.False(((Logic)res4).Condition);
    }

    [Fact]
    public void And_Works()
    {
        var (res1, _) = Run("true and true");
        Assert.True(((Logic)res1).Condition);

        var (res2, _) = Run("true and false");
        Assert.False(((Logic)res2).Condition);

        var (res3, _) = Run("10 and 20");
        Assert.True(((Logic)res3).Condition);

        var (res4, _) = Run("and? true false");
        Assert.False(((Logic)res4).Condition);
    }

    [Fact]
    public void Or_Works()
    {
        var (res1, _) = Run("true or false");
        Assert.True(((Logic)res1).Condition);

        var (res2, _) = Run("false or none");
        Assert.False(((Logic)res2).Condition);

        var (res3, _) = Run("none or 10");
        Assert.True(((Logic)res3).Condition);

        var (res4, _) = Run("or? false true");
        Assert.True(((Logic)res4).Condition);
    }

    [Fact]
    public void Xor_Works()
    {
        var (res1, _) = Run("true xor false");
        Assert.True(((Logic)res1).Condition);

        var (res2, _) = Run("true xor true");
        Assert.False(((Logic)res2).Condition);

        var (res3, _) = Run("false xor false");
        Assert.False(((Logic)res3).Condition);

        var (res4, _) = Run("xor? true false");
        Assert.True(((Logic)res4).Condition);
    }

    [Fact]
    public void None_Question_Works()
    {
        var (res1, _) = Run("none? none");
        Assert.True(((Logic)res1).Condition);

        var (res2, _) = Run("none? 10");
        Assert.False(((Logic)res2).Condition);
    }

    [Fact]
    public void Not_Equal_Question_Works()
    {
        var (res1, _) = Run("not-equal? 10 20");
        Assert.True(((Logic)res1).Condition);

        var (res2, _) = Run("not-equal? 10 10");
        Assert.False(((Logic)res2).Condition);
    }

    [Fact]
    public void Zero_Question_Works()
    {
        var (res1, _) = Run("zero? 0");
        Assert.True(((Logic)res1).Condition);

        var (res2, _) = Run("zero? 10");
        Assert.False(((Logic)res2).Condition);
    }
}
