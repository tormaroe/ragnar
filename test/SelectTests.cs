using Xunit;
using Ragnar;

namespace Ragnar.Tests;

public class SelectTests : TestBase
{
    [Fact]
    public void TestSelectBlock()
    {
        var result = Run("blk: [red 123 green 456 blue 789] select blk 'red").Result;
        Assert.Equal(123, ((Integer)result).Number);
        
        result = Run("blk: [red 123 green 456 blue 789] select blk 'green").Result;
        Assert.Equal(456, ((Integer)result).Number);
        
        result = Run("blk: [red 123 green 456 blue 789] select blk 'blue").Result;
        Assert.Equal(789, ((Integer)result).Number);
    }

    [Fact]
    public void TestSelectNotFound()
    {
        var result = Run("blk: [red 123] select blk 'yellow").Result;
        Assert.True(result is Word w && w.Name == "none");
    }

    [Fact]
    public void TestSelectLast()
    {
        var result = Run("blk: [red 123] select blk 123").Result;
        Assert.True(result is Word w && w.Name == "none");
    }

    [Fact]
    public void TestSelectText()
    {
        var result = Run("select \"abcdef\" \"c\"").Result;
        Assert.Equal("d", ((Text)result).Content);

        result = Run("select \"abcdef\" \"f\"").Result;
        Assert.True(result is Word wn1 && wn1.Name == "none");

        result = Run("select \"abcdef\" \"z\"").Result;
        Assert.True(result is Word wn2 && wn2.Name == "none");
    }

    [Fact]
    public void TestSelectObject()
    {
        var result = Run("obj: make object! [a: 1 b: 2] select obj 'a").Result;
        Assert.Equal(1, ((Integer)result).Number);

        result = Run("obj: make object! [a: 1 b: 2] select obj 'b").Result;
        Assert.Equal(2, ((Integer)result).Number);

        result = Run("obj: make object! [a: 1 b: 2] select obj 'c").Result;
        Assert.True(result is Word wn && wn.Name == "none");
    }

    [Fact]
    public void TestSwitch()
    {
        var result = Run("switch 'b [a [1] b [2] c [3]]").Result;
        Assert.Equal(2, ((Integer)result).Number);

        result = Run("switch 'd [a [1] b [2] c [3]]").Result;
        Assert.True(result is Word w1 && w1.Name == "none");
    }

    [Fact]
    public void TestSwitchDefault()
    {
        var result = Run("switch/default 'd [a [1] b [2]] [99]").Result;
        Assert.Equal(99, ((Integer)result).Number);
    }
}
