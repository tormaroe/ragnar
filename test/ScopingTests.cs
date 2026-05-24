using Xunit;
using Ragnar;
using System;

namespace Ragnar.Tests;

public class ScopingTests : TestBase
{
    [Fact]
    public void PlainAssignmentInFunctionShouldBeLocalByDefault()
    {
        var code = @"
            x: 10
            f: func [] [
                x: 20
                x
            ]
            res: f
            reduce [x res]
        ";

        var result = (Block)Run(code).Result;
        Assert.Equal(10, ((Integer)result.Children[0]).Number);
        Assert.Equal(20, ((Integer)result.Children[1]).Number);
    }

    [Fact]
    public void SetNativeInFunctionShouldMutateGlobal()
    {
        var code = @"
            x: 10
            f: func [] [
                set 'x 20
                x
            ]
            res: f
            reduce [x res]
        ";

        var result = (Block)Run(code).Result;
        Assert.Equal(20, ((Integer)result.Children[0]).Number);
        Assert.Equal(20, ((Integer)result.Children[1]).Number);
    }

    [Fact]
    public void NestedLexicalScopesShouldUpdateLexicalParentCorrectly()
    {
        var code = @"
            make-counter: func [start] [
                count: start
                func [] [
                    count: count + 1
                    count
                ]
            ]
            c1: make-counter 10
            c1
            c1
        ";

        var result = (Integer)Run(code).Result;
        Assert.Equal(12, result.Number);
    }

    [Fact]
    public void UseFunctionShouldCreateLocalVariables()
    {
        var code = @"
            x: 10
            use [x y] [
                x: 20
                y: 30
            ]
            x
        ";

        var result = (Integer)Run(code).Result;
        Assert.Equal(10, result.Number);
    }

    [Fact]
    public void UseFunctionShouldSupportSingleWordSyntax()
    {
        var code = @"
            x: 10
            use 'x [
                x: 20
            ]
            x
        ";

        var result = (Integer)Run(code).Result;
        Assert.Equal(10, result.Number);
    }

    [Fact]
    public void ClosuresWithinUseShouldResolveLexicalVars()
    {
        var code = @"
            my-closure: none
            use [counter] [
                counter: 0
                my-closure: func [] [
                    counter: counter + 1
                    counter
                ]
            ]
            my-closure
            my-closure
        ";

        var result = (Integer)Run(code).Result;
        Assert.Equal(2, result.Number);
    }

    [Fact]
    public void DoesFunctionShouldSupportLocalScope()
    {
        var code = @"
            x: 10
            f: does [
                x: 20
                x
            ]
            res: f
            reduce [x res]
        ";

        var result = (Block)Run(code).Result;
        Assert.Equal(10, ((Integer)result.Children[0]).Number);
        Assert.Equal(20, ((Integer)result.Children[1]).Number);
    }

    [Fact]
    public void LetFunctionShouldCreateLocalVariablesWithInitialValues()
    {
        var code = @"
            x: 10
            res: let [x 20 y 30] [
                x + y
            ]
            reduce [x res]
        ";

        var result = (Block)Run(code).Result;
        Assert.Equal(10, ((Integer)result.Children[0]).Number);
        Assert.Equal(50, ((Integer)result.Children[1]).Number);
    }

    [Fact]
    public void LetFunctionShouldSupportSequentialEvaluation()
    {
        var code = @"
            res: let [x 10 y x + 5] [
                y
            ]
            res
        ";

        var result = (Integer)Run(code).Result;
        Assert.Equal(15, result.Number);
    }

    [Fact]
    public void LetFunctionShouldBindSharedLexicalContextForFunctions()
    {
        var code = @"
            f: let [
                counter 0
                increment func [] [
                    counter: counter + 1
                    counter
                ]
            ] [
                :increment
            ]
            f
            f
        ";

        var result = (Integer)Run(code).Result;
        Assert.Equal(2, result.Number);
    }
}
