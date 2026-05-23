
using Xunit;
using Ragnar;

namespace Ragnar.Tests;

public class MezzanineTests : TestBase
{
    [Fact]
    public void Test_Enumerate_Word_Block()
    {
        string script = @"
            vars: call-static ""System.Environment"" ""GetEnvironmentVariables"" []
            result: []
            enumerate vars item [
                append result item/key
            ]
            length? result
        ";

        var result = (Integer)Run(script).Result;
        Assert.True(result.Number > 0);
    }

    [Fact]
    public void Test_List_Env()
    {
        string script = @"
            env: list-env
            length? env
        ";

        var result = (Integer)Run(script).Result;
        Assert.True(result.Number > 0);
        Assert.True(result.Number % 2 == 0); // Name-value pairs
    }

    [Fact]
    public void Test_MapEach_SingleWord()
    {
        string script = @"
            map-each x [1 2 3] [x * 2]
        ";
        var (result, _) = Run(script);
        Assert.Equal("[ 2 4 6 ]", result.ToString());
    }

    [Fact]
    public void Test_MapEach_BlockOfWords()
    {
        string script = @"
            map-each [a b] [1 2 3 4] [a + b]
        ";
        var (result, _) = Run(script);
        Assert.Equal("[ 3 7 ]", result.ToString());
    }

    [Fact]
    public void Test_MapEach_IncompleteData()
    {
        string script = @"
            map-each [a b] [1 2 3] [reduce [a b]]
        ";
        var (result, _) = Run(script);
        Assert.Equal("[ [ 1 2 ] [ 3 none ] ]", result.ToString());
    }

    [Fact]
    public void Test_MapEach_Scoping()
    {
        string script = @"
            x: 100
            y: 200
            res: map-each [x y] [1 2 3 4] [x + y]
            reduce [x y res]
        ";
        var (result, _) = Run(script);
        Assert.Equal("[ 100 200 [ 3 7 ] ]", result.ToString());
    }

    [Fact]
    public void Test_MapEach_Continue()
    {
        string script = @"
            map-each x [1 2 3 4] [
                either x = 3 [continue] [x * 10]
            ]
        ";
        var (result, _) = Run(script);
        Assert.Equal("[ 10 20 40 ]", result.ToString());
    }

    [Fact]
    public void Test_MapEach_Break()
    {
        string script = @"
            map-each x [1 2 3 4] [
                either x = 3 [break] [x * 10]
            ]
        ";
        var (result, _) = Run(script);
        Assert.Equal("[ 10 20 ]", result.ToString());
    }
}

