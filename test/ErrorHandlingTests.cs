using Xunit;

namespace Ragnar.Tests;

public class ErrorHandlingTests : TestBase
{
    [Fact]
    public void Try_Success_ReturnsValue()
    {
        var (result, _) = Run("try [1 + 2]");
        Assert.Equal("3", result.ToUserString());
    }

    [Fact]
    public void Try_Error_ReturnsErrorValue()
    {
        var (result, _) = Run("try [1 / 0]");
        Assert.IsType<ErrorValue>(result);
        Assert.Contains("** Script Error:", result.ToString());
    }

    [Fact]
    public void Attempt_Success_ReturnsValue()
    {
        var (result, _) = Run("attempt [1 + 2]");
        Assert.Equal("3", result.ToUserString());
    }

    [Fact]
    public void Attempt_Error_ReturnsNone()
    {
        var (result, _) = Run("attempt [1 / 0]");
        Assert.Equal("none", result.ToUserString());
    }

    [Fact]
    public void Catch_NoThrow_ReturnsResult()
    {
        var (result, _) = Run("catch [1 + 2]");
        Assert.Equal("3", result.ToUserString());
    }

    [Fact]
    public void Catch_WithThrow_ReturnsThrownValue()
    {
        var (result, _) = Run("catch [1 + 2 throw 42 3 + 4]");
        Assert.Equal("42", result.ToUserString());
    }

    [Fact]
    public void Throw_WithoutCatch_ThrowsException()
    {
        Assert.Throws<ThrowException>(() => Run("throw 10"));
    }
}
