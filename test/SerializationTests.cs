using Xunit;
using Ragnar;

namespace Ragnar.Tests;

public class SerializationTests : TestBase
{
    [Fact]
    public void Load_From_String()
    {
        var code = @"load ""[1 2 3]""";
        var result = Run(code).Result as Block;
        Assert.Equal(3, result.Children.Count);
        Assert.Equal(1, (result.Children[0] as Integer).Number);
    }

    [Fact]
    public void Save_To_String()
    {
        var code = @"
            s: """"
            save s [1 2 3]
            s
        ";
        var result = Run(code).Result as Text;
        Assert.Equal("[ 1 2 3 ]", result.Content);
    }

    [Fact]
    public void Round_Trip_File()
    {
        string tempFile = System.IO.Path.GetTempFileName();
        try
        {
            var code = $@"
                val: [a: 10 b: 20]
                save %{tempFile} val
                load %{tempFile}
            ";
            var result = Run(code).Result as Block;
            Assert.Equal(4, result.Children.Count);
            Assert.Equal("a", (result.Children[0] as SetWord).Name);
            Assert.Equal(10, (result.Children[1] as Integer).Number);
        }
        finally
        {
            if (System.IO.File.Exists(tempFile)) System.IO.File.Delete(tempFile);
        }
    }

    [Fact]
    public void Round_Trip_Complex()
    {
        var code = @"
            val: [10 20.5 ""hello"" 'lit /ref :get word:]
            s: """"
            save s val
            load s
        ";
        var result = Run(code).Result as Block;
        Assert.Equal(7, result.Children.Count);
        Assert.Equal(10, (result.Children[0] as Integer).Number);
        Assert.Equal(20.5, (result.Children[1] as Decimal).Number);
        Assert.Equal("hello", (result.Children[2] as Text).Content);
        Assert.Equal("lit", (result.Children[3] as LitWord).Name);
        Assert.Equal("ref", (result.Children[4] as Refinement).Name);
        Assert.Equal("get", (result.Children[5] as GetWord).Name);
        Assert.Equal("word", (result.Children[6] as SetWord).Name);
    }

    [Fact]
    public void Round_Trip_Object()
    {
        var code = @"
            obj: make object! [a: 10 b: ""hello""]
            s: """"
            save s obj
            loaded: do load s
            loaded/a
        ";
        var result = Run(code).Result as Integer;
        Assert.Equal(10, result.Number);
    }

    [Fact]
    public void Round_Trip_Function()
    {
        string tempFile = System.IO.Path.GetTempFileName();
        try
        {
            var code = $@"
                f: func [""test"" x /with y] [either with [x + y] [x]]
                save %{tempFile} :f
                loaded: do load %{tempFile}
                reduce [loaded 10 loaded/with 10 5]
            ";
            var result = Run(code).Result as Block;
            Assert.Equal(10, (result.Children[0] as Integer).Number);
            Assert.Equal(15, (result.Children[1] as Integer).Number);
        }
        finally
        {
            if (System.IO.File.Exists(tempFile)) System.IO.File.Delete(tempFile);
        }
    }
}
