using Xunit;
using Ragnar;

namespace Ragnar.Tests;

public class SerializationTests : TestBase
{
    [Fact]
    public void Load_From_String()
    {
        var code = @"load ""[1 2 3]""";
        var result = (Block)Run(code).Result;
        Assert.Equal(3, result.Children.Count);
        Assert.Equal(1, ((Integer)result.Children[0]).Number);
    }

    [Fact]
    public void Save_To_String()
    {
        var code = @"
            s: """"
            save s [1 2 3]
            s
        ";
        var result = (Text)Run(code).Result;
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
            var result = (Block)Run(code).Result;
            Assert.Equal(4, result.Children.Count);
            Assert.Equal("a", ((SetWord)result.Children[0]).Name);
            Assert.Equal(10, ((Integer)result.Children[1]).Number);
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
        var result = (Block)Run(code).Result;
        Assert.Equal(7, result.Children.Count);
        Assert.Equal(10, ((Integer)result.Children[0]).Number);
        Assert.Equal(20.5, ((Decimal)result.Children[1]).Number);
        Assert.Equal("hello", ((Text)result.Children[2]).Content);
        Assert.Equal("lit", ((LitWord)result.Children[3]).Name);
        Assert.Equal("ref", ((Refinement)result.Children[4]).Name);
        Assert.Equal("get", ((GetWord)result.Children[5]).Name);
        Assert.Equal("word", ((SetWord)result.Children[6]).Name);
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
        var result = (Integer)Run(code).Result;
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
            var result = (Block)Run(code).Result;
            Assert.Equal(10, ((Integer)result.Children[0]).Number);
            Assert.Equal(15, ((Integer)result.Children[1]).Number);
        }
        finally
        {
            if (System.IO.File.Exists(tempFile)) System.IO.File.Delete(tempFile);
        }
    }
}
