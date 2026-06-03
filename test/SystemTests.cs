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

    [Fact]
    public void Call_Pid_And_Process_Management_Works()
    {
        bool isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
        string cmd = isWindows ? "ping 127.0.0.1 -n 10" : "sleep 10";

        var pidResult = Run($"call/pid \"{cmd}\"").Result;
        Assert.IsType<Integer>(pidResult);
        long pid = ((Integer)pidResult).Number;
        Assert.True(pid > 0);

        // Check if process is alive
        var statusResult = Run($"proc-status {pid}").Result;
        Assert.IsType<Logic>(statusResult);
        Assert.True(((Logic)statusResult).Condition);

        // Kill process
        var killResult = Run($"proc-kill {pid}").Result;
        Assert.IsType<Logic>(killResult);
        Assert.True(((Logic)killResult).Condition);

        // Check if process is now dead
        var statusAfterKill = Run($"proc-status {pid}").Result;
        Assert.IsType<Logic>(statusAfterKill);
        Assert.False(((Logic)statusAfterKill).Condition);
    }

    [Fact]
    public void Proc_Status_And_Kill_Handle_Invalid_Pid()
    {
        var statusResult = Run("proc-status 999999").Result;
        Assert.IsType<Logic>(statusResult);
        Assert.False(((Logic)statusResult).Condition);

        var killResult = Run("proc-kill 999999").Result;
        Assert.IsType<Logic>(killResult);
        Assert.False(((Logic)killResult).Condition);
    }

    [Fact]
    public void Read_Can_Read_Locked_File()
    {
        string path = "test-locked.txt";
        System.IO.File.WriteAllText(path, "locked file content");

        // Open file with FileAccess.Write and FileShare.ReadWrite (simulating another process writing/locking the file)
        using (var lockStream = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Write, System.IO.FileShare.ReadWrite))
        {
            // Try to read the file in Ragnar
            var readResult = Run($"read %{path}").Result;
            Assert.IsType<Text>(readResult);
            Assert.Equal("locked file content", ((Text)readResult).Content);

            var readLinesResult = Run($"read/lines %{path}").Result;
            Assert.IsType<Block>(readLinesResult);
            var block = (Block)readLinesResult;
            Assert.Single(block.Children);
            Assert.Equal("locked file content", ((Text)block.Children[0]).Content);
        }

        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }
    }
}
