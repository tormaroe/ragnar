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
}
