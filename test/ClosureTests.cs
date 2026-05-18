using Xunit;

namespace Ragnar.Tests;

public class ClosureTests : TestBase
{
    [Fact]
    public void FunctionShouldFormClosure()
    {
        var code = @"
            make-adder: func [x] [
                y: x
                func [z] [
                    y: y + z
                ]
            ]
            adder: make-adder 10
            adder 5
        ";
        var result = (Integer)Run(code).Result;
        Assert.Equal(15, result.Number);
    }

    [Fact]
    public void MultipleClosuresShouldHaveIndependentState()
    {
        var code = @"
            make-counter: func [start] [
                count: start
                func [] [
                    count: count + 1
                    count
                ]
            ]
            c1: make-counter 0
            c2: make-counter 100
            c1
            c1
            c2
            c1
        ";
        // c1 -> 1, then 2
        // c2 -> 101
        // c1 -> 3
        var result = (Integer)Run(code).Result;
        Assert.Equal(3, result.Number);
    }
}
