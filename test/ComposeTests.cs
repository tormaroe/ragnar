using Xunit;

namespace Ragnar.Tests;

public class ComposeTests : TestBase
{
    [Fact]
    public void Compose_ReplacesParens()
    {
        var (result, _) = Run("compose [1 (1 + 1) 3]");
        Assert.Equal("[ 1 2 3 ]", result.ToString());
    }

    [Fact]
    public void Compose_SplicesBlocks()
    {
        var (result, _) = Run("compose [a (reduce [1 2 3]) b]");
        Assert.Equal("[ a 1 2 3 b ]", result.ToString());
    }

    [Fact]
    public void Compose_DoesNotEvaluateWords()
    {
        var (result, _) = Run("x: 10 compose [x (x)]");
        Assert.Equal("[ x 10 ]", result.ToString());
    }

    [Fact]
    public void Compose_NestedParens()
    {
        var (result, _) = Run("compose [1 ( (2 * 3) ) 7]");
        Assert.Equal("[ 1 6 7 ]", result.ToString());
    }
}
