using Xunit;
using Ragnar;

namespace Ragnar.Tests;

public class PipingTests : TestBase
{
    [Fact]
    public void Pipe_Arity_1_Works()
    {
        var code = @"
            double: func [x] [x * 2]
            10 | :double
        ";
        var (result, _) = Run(code);
        Assert.Equal(20, ((Integer)result).Number);
    }

    [Fact]
    public void Pipe_Arity_2_First_Arg_Works()
    {
        var code = @"
            sub_func: func [a b] [a - b]
            10 |> :sub_func 3
        ";
        var (result, _) = Run(code);
        Assert.Equal(7, ((Integer)result).Number);
    }

    [Fact]
    public void Pipe_Arity_2_Second_Arg_Works()
    {
        var code = @"
            sub_func: func [a b] [a - b]
            3 >| :sub_func 10
        ";
        var (result, _) = Run(code);
        Assert.Equal(7, ((Integer)result).Number);
    }

    [Fact]
    public void Pipe_Arity_3_First_Arg_Works()
    {
        var code = @"
            math_func: func [a b c] [a * b + c]
            5 |>> :math_func 3 2
        ";
        var (result, _) = Run(code);
        Assert.Equal(17, ((Integer)result).Number);
    }

    [Fact]
    public void Pipe_Arity_3_Second_Arg_Works()
    {
        var code = @"
            math_func: func [a b c] [a * b + c]
            3 >|> :math_func 5 2
        ";
        var (result, _) = Run(code);
        Assert.Equal(17, ((Integer)result).Number);
    }

    [Fact]
    public void Pipe_Arity_3_Third_Arg_Works()
    {
        var code = @"
            math_func: func [a b c] [a * b + c]
            2 >>| :math_func 5 3
        ";
        var (result, _) = Run(code);
        Assert.Equal(17, ((Integer)result).Number);
    }

    [Fact]
    public void Chaining_Pipes_Works()
    {
        var code = @"
            double: func [x] [x * 2]
            add_five: func [x] [x + 5]
            10 | :double | :add_five | :double
        ";
        var (result, _) = Run(code);
        Assert.Equal(50, ((Integer)result).Number);
    }

    [Fact]
    public void Piping_Preserves_It_During_Paren_Argument_Evaluation()
    {
        var code = @"
            sub_func: func [a b] [a - b]
            10 |> :sub_func (1 + 2)
        ";
        var (result, _) = Run(code);
        // This is expected to succeed (return 7) because paren evaluation (1 + 2) 
        // does not permanently overwrite 'it' in the caller context.
        Assert.Equal(7, ((Integer)result).Number);
    }

    [Fact]
    public void Piping_Preserves_It_During_Do_Block_Argument_Evaluation()
    {
        var code = @"
            sub_func: func [a b] [a - b]
            10 |> :sub_func do [1 + 2]
        ";
        var (result, _) = Run(code);
        // This is expected to succeed (return 7) because do block evaluation 
        // does not permanently overwrite 'it' in the caller context.
        Assert.Equal(7, ((Integer)result).Number);
    }
}

