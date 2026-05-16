using Xunit;
using Ragnar;
using System.Collections.Generic;

namespace Ragnar.Tests;

public class FunctionRefinementTests : TestBase
{
    [Fact]
    public void Basic_Refinement_No_Args()
    {
        var code = @"
            f: func [a /b] [
                either b [a + 1] [a]
            ]
            reduce [f 10 f/b 10]
        ";
        var result = Run(code).Result as Block;
        Assert.Equal(10, (result.Children[0] as Integer).Number);
        Assert.Equal(11, (result.Children[1] as Integer).Number);
    }

    [Fact]
    public void Refinement_With_Args()
    {
        var code = @"
            f: func [a /with b] [
                either with [a + b] [a]
            ]
            reduce [f 10 f/with 10 5]
        ";
        var result = Run(code).Result as Block;
        Assert.Equal(10, (result.Children[0] as Integer).Number);
        Assert.Equal(15, (result.Children[1] as Integer).Number);
    }

    [Fact]
    public void Multiple_Refinements()
    {
        var code = @"
            f: func [/add a /sub b] [
                res: 10
                if add [res: res + a]
                if sub [res: res - b]
                res
            ]
            reduce [f f/add 5 f/sub 3 f/add/sub 5 3]
        ";
        var result = Run(code).Result as Block;
        Assert.Equal(10, (result.Children[0] as Integer).Number);
        Assert.Equal(15, (result.Children[1] as Integer).Number);
        Assert.Equal(7, (result.Children[2] as Integer).Number);
        Assert.Equal(12, (result.Children[3] as Integer).Number);
    }

    [Fact]
    public void Early_Return()
    {
        var code = @"
            f: func [n] [
                if n > 10 [return ""big""]
                ""small""
            ]
            reduce [f 5 f 15]
        ";
        var result = Run(code).Result as Block;
        Assert.Equal("small", (result.Children[0] as Text).Content);
        Assert.Equal("big", (result.Children[1] as Text).Content);
    }

    [Fact]
    public void Early_Exit()
    {
        var code = @"
            f: func [n] [
                if n > 10 [exit]
                ""small""
            ]
            reduce [f 5 f 15]
        ";
        var result = Run(code).Result as Block;
        Assert.Equal("small", (result.Children[0] as Text).Content);
        Assert.Equal("none", (result.Children[1] as Word).Name);
    }

    [Fact]
    public void Refinement_Arg_Order_Matches_Path()
    {
        var code = @"
            f: func [/a x /b y] [
                reduce [x y]
            ]
            reduce [f/a/b 1 2 f/b/a 1 2]
        ";
        var result = Run(code).Result as Block;
        
        // f/a/b 1 2 -> a is true, x is 1, b is true, y is 2
        var res1 = result.Children[0] as Block;
        Assert.Equal(1, (res1.Children[0] as Integer).Number);
        Assert.Equal(2, (res1.Children[1] as Integer).Number);

        // f/b/a 1 2 -> b is true, y is 1, a is true, x is 2
        var res2 = result.Children[1] as Block;
        Assert.Equal(2, (res2.Children[0] as Integer).Number);
        Assert.Equal(1, (res2.Children[1] as Integer).Number);
    }
}
