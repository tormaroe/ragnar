using Xunit;
using Ragnar;

namespace Ragnar.Tests;

public class MathTests : TestBase
{
    [Fact]
    public void Random_Returns_Value_In_Range()
    {
        var (result, _) = Run("random 10");
        var val = Assert.IsType<Integer>(result).Number;
        Assert.True(val >= 1 && val <= 10);
    }

    [Fact]
    public void Random_Decimal_Returns_Value_In_Range()
    {
        var (result, _) = Run("random 1.0");
        var val = Assert.IsType<Decimal>(result).Number;
        Assert.True(val >= 0.0 && val <= 1.0);
    }

    [Fact]
    public void Random_Seed_Produces_Deterministic_Sequence()
    {
        // Seed once and get a sequence
        Run("random/seed 42");
        var (res1, _) = Run("random 1000");
        var (res2, _) = Run("random 1000");

        // Seed again with same value and check sequence matches
        Run("random/seed 42");
        var (res3, _) = Run("random 1000");
        var (res4, _) = Run("random 1000");

        Assert.Equal(((Integer)res1).Number, ((Integer)res3).Number);
        Assert.Equal(((Integer)res2).Number, ((Integer)res4).Number);
    }

    [Fact]
    public void Max_Returns_Greater_Value()
    {
        var (res1, _) = Run("max 10 20");
        Assert.Equal(20, ((Integer)res1).Number);

        var (res2, _) = Run("max 100 50");
        Assert.Equal(100, ((Integer)res2).Number);
    }

    [Fact]
    public void Min_Returns_Lesser_Value()
    {
        var (res1, _) = Run("min 10 20");
        Assert.Equal(10, ((Integer)res1).Number);

        var (res2, _) = Run("min 100 50");
        Assert.Equal(50, ((Integer)res2).Number);
    }

    [Fact]
    public void Negate_Multiplies_By_Negative_One()
    {
        var (res1, _) = Run("negate 10");
        Assert.Equal(-10, ((Integer)res1).Number);

        var (res2, _) = Run("negate -50");
        Assert.Equal(50, ((Integer)res2).Number);
    }
}
