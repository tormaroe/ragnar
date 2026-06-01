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

    [Fact]
    public void Home_Native_Works()
    {
        var result = Run("home").Result;
        Assert.IsType<File>(result);
        Assert.Equal(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ((File)result).Path);
    }

    [Fact]
    public void Exists_Mezzanine_Works()
    {
        Run("write %test-exists.txt \"hello\"");
        try
        {
            var result1 = Run("exists? %test-exists.txt").Result;
            Assert.IsType<Logic>(result1);
            Assert.True(((Logic)result1).Condition);

            var result2 = Run("exists? %non-existent-file-12345.txt").Result;
            Assert.IsType<Logic>(result2);
            Assert.False(((Logic)result2).Condition);
        }
        finally
        {
            if (System.IO.File.Exists("test-exists.txt"))
            {
                System.IO.File.Delete("test-exists.txt");
            }
        }
    }

    [Fact]
    public void Set_Get_Env_Mezzanine_Works()
    {
        Run("set-env \"RAGNAR_TEST_VAR\" \"hello-ragnar\"");
        var result1 = Run("get-env \"RAGNAR_TEST_VAR\"").Result;
        Assert.IsType<Text>(result1);
        Assert.Equal("hello-ragnar", ((Text)result1).Content);

        Run("set-env \"RAGNAR_TEST_VAR\" none");
        var result2 = Run("get-env \"RAGNAR_TEST_VAR\"").Result;
        Assert.IsType<Word>(result2);
        Assert.Equal("none", ((Word)result2).Name);
    }

    [Fact]
    public void Call_Capture_Output_Works()
    {
        bool isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
        
        // Use a basic command that prints text
        string cmd = isWindows ? "cmd /c echo hello" : "echo hello";
        
        var result = Run($"call/output \"{cmd}\"").Result;
        Assert.IsType<Text>(result);
        Assert.Contains("hello", ((Text)result).Content);
    }
}
