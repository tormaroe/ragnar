using Xunit;
using Ragnar;

namespace Ragnar.Tests;

public class SystemTests : TestBase
{
    [Fact]
    public void System_Object_Exists_In_Global_Context()
    {
        var result = Run("system/console/prompt").Result;
        Assert.Equal(">> ", result.ToUserString());
    }

    [Fact]
    public void System_Object_Can_Be_Modified()
    {
        var result = Run("system/console/prompt: \"Input: \" system/console/prompt").Result;
        Assert.Equal("Input: ", result.ToUserString());
    }

    [Fact]
    public void Reform_Function_Works()
    {
        var result = Run("reform [1 2 3]").Result;
        Assert.Equal("1 2 3", result.ToUserString());
    }

    [Fact]
    public void Now_Function_Works()
    {
        var result = Run("now/year").Result;
        Assert.Equal(DateTime.Now.Year.ToString(), result.ToUserString());
    }
}
