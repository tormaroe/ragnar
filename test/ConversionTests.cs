using Xunit;
using Ragnar;
using System;

namespace Ragnar.Tests;

public class ConversionTests : TestBase
{
    [Fact]
    public void To_Integer_Works()
    {
        var (res1, _) = Run("to-integer \"123\"");
        Assert.Equal(123, ((Integer)res1).Number);

        var (res2, _) = Run("to-integer 12.3");
        Assert.Equal(12, ((Integer)res2).Number);
    }

    [Fact]
    public void To_Decimal_Works()
    {
        var (res1, _) = Run("to-decimal \"12.3\"");
        Assert.Equal(12.3, ((Decimal)res1).Number);

        var (res2, _) = Run("to-decimal 12");
        Assert.Equal(12.0, ((Decimal)res2).Number);
    }

    [Fact]
    public void To_String_Works()
    {
        var (res1, _) = Run("to-string 123");
        Assert.Equal("123", ((Text)res1).ToUserString());

        var (res2, _) = Run("to-string true");
        Assert.Equal("true", ((Text)res2).ToUserString());
    }

    [Fact]
    public void Mold_Works()
    {
        var (res1, _) = Run("mold 123");
        Assert.Equal("123", ((Text)res1).ToUserString());

        var (res2, _) = Run("mold [x: 1]");
        Assert.Equal("[ x: 1 ]", ((Text)res2).ToUserString());

        var (res3, _) = Run("mold/only [x: 1]");
        Assert.Equal("x: 1", ((Text)res3).ToUserString());
    }
}
