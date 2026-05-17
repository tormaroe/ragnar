using Xunit;
using Ragnar;

namespace Ragnar.Tests;

public class CompositionTests : TestBase
{
    [Fact]
    public void TestForwardComposition()
    {
        var script = @"
            inc: func [n] [n + 1]
            double: func [n] [n * 2]
            f: :inc >> :double
            f 5
        ";
        var (result, _) = Run(script);
        Assert.Equal(12, ((Integer)result).Number);
    }

    [Fact]
    public void TestBackwardComposition()
    {
        var script = @"
            inc: func [n] [n + 1]
            double: func [n] [n * 2]
            f: :inc << :double
            f 5
        ";
        var (result, _) = Run(script);
        Assert.Equal(11, ((Integer)result).Number);
    }

    [Fact]
    public void TestChainedComposition()
    {
        var script = @"
            inc: func [n] [n + 1]
            double: func [n] [n * 2]
            square: func [n] [n * n]
            f: :inc >> :double >> :square
            f 5 ; (5 + 1) * 2 = 12, 12 * 12 = 144
        ";
        var (result, _) = Run(script);
        Assert.Equal(144, ((Integer)result).Number);
    }

    [Fact]
    public void TestNativeComposition()
    {
        var script = @"
            f: :to-integer >> :abs
            f ""-123""
        ";
        var (result, _) = Run(script);
        Assert.Equal(123, ((Integer)result).Number);
    }

    [Fact]
    public void TestPartialNative()
    {
        var script = @"
            f: partial :add 2
            f 3
        ";
        var (result, _) = Run(script);
        Assert.Equal(5, ((Integer)result).Number);
    }

    [Fact]
    public void TestPartialFunction()
    {
        var script = @"
            h: func [a b c] [a + (b * c)]
            f: partial :h 10
            f 2 3 ; 10 + (2 * 3) = 16
        ";
        var (result, _) = Run(script);
        Assert.Equal(16, ((Integer)result).Number);
    }

    [Fact]
    public void TestPartialAndCompose()
    {
        var script = @"
            f: (partial :add 2) >> :abs
            f -5 ; abs (add 2 -5) = abs -3 = 3
        ";
        var (result, _) = Run(script);
        Assert.Equal(3, ((Integer)result).Number);
    }
}
