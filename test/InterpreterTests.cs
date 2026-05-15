
namespace Ragnar.Tests;

public class InterpreterTests : TestBase
{
    [Fact]
    public void Basic_Math_And_Assignment_Works()
    {
        var (result, ctx) = Run("a: 10 b: 20 add a b");

        // 1. Check the return value of the whole block (the last expression)
        var intResult = Assert.IsType<Integer>(result);
        Assert.Equal(30, intResult.Number);

        // 2. Check that the side-effects (assignments) stayed in context
        var aVal = Assert.IsType<Integer>(ctx.Get("a"));
        Assert.Equal(10, aVal.Number);
    }

    [Fact]
    public void If_Statement_Executes_Correct_Branch()
    {
        // Test True Case
        var (resTrue, _) = Run("if true [ 42 ]");
        Assert.Equal(42, Assert.IsType<Integer>(resTrue).Number);

        // Test False Case (should return 'none' word as per our implementation)
        var (resFalse, _) = Run("if false [ 42 ]");
        Assert.Equal("none", Assert.IsType<Word>(resFalse).Name);
    }

    [Fact]
    public void User_Defined_Functions_Pass_Arguments_Locally()
    {
        // Define a function, then call it. 
        // We also check that the local 'n' doesn't leak to global context.
        var code = @"
            increment: func [n] [ add n 1 ]
            result: increment 99
        ";

        var (lastValue, ctx) = Run(code);

        var resultVal = Assert.IsType<Integer>(ctx.Get("result"));
        Assert.Equal(100, resultVal.Number);

        // n should not exist in the global context
        Assert.Throws<Exception>(() => ctx.Get("n"));
    }

    [Fact]
    public void Get_Word_Allows_Function_Aliasing()
    {
        // Get the 'add' native without executing it and assign it to 'plus'
        var code = @"
            plus: :add
            plus 5 5
        ";

        var (result, _) = Run(code);

        Assert.Equal(10, Assert.IsType<Integer>(result).Number);
    }

    [Fact]
    public void Nested_Blocks_And_Do_Execution()
    {
        // 'do' should evaluate the contents of a block
        var (result, _) = Run("do [ add 5 5 ]");

        Assert.Equal(10, Assert.IsType<Integer>(result).Number);
    }

    [Fact]
    public void Lexer_Ignores_Comments()
    {
        var code = @"
            a: 10 ; this is a comment
            ; another comment on its own line
            b: 20
            add a b ; result should be 30
        ";

        var (result, _) = Run(code);

        var intVal = Assert.IsType<Integer>(result);
        Assert.Equal(30, intVal.Number);
    }

    [Fact]
    public void Equality_Check_Returns_Logic()
    {
        var (result, _) = Run("equal? 10 10");

        var logicResult = Assert.IsType<Logic>(result);
        Assert.True(logicResult.Condition);
    }

    [Fact]
    public void Loop_Executes_Multiple_Times()
    {
        var code = @"
            counter: 0
            loop 5 [ counter: add counter 1 ]
            counter
        ";
        var (result, _) = Run(code);
        Assert.Equal(5, Assert.IsType<Integer>(result).Number);
    }

    [Fact]
    public void While_Executes_Until_Condition_Is_False()
    {
        var code = @"
            n: 5
            total: 0
            while [ greater? n 0 ] [
                total: add total n
                n: sub n 1
            ]
            total
        ";
        // 5 + 4 + 3 + 2 + 1 = 15
        var (result, _) = Run(code);
        Assert.Equal(15, Assert.IsType<Integer>(result).Number);
    }

    [Fact]
    public void Foreach_Iterates_Over_Block()
    {
        var code = @"
            sum: 0
            foreach n [1 2 3 4] [
                sum: add sum n
            ]
            sum
        ";
        var (result, _) = Run(code);
        Assert.Equal(10, Assert.IsType<Integer>(result).Number);
    }

    [Fact]
    public void LitWord_Evaluates_To_Word()
    {
        // In code: 'my-variable
        // The result should be the Word object itself, not its value.
        var (result, _) = Run("'test-word");

        var wordResult = Assert.IsType<Word>(result);
        Assert.Equal("test-word", wordResult.Name);
    }

    [Fact]
    public void LitWord_Prevents_Immediate_Evaluation()
    {
        // If we use a regular word that isn't defined, it throws an error.
        // If we use a lit-word, it should succeed because it doesn't look it up yet.
        var (result, _) = Run("'undefined-word");

        Assert.IsType<Word>(result);
    }

    [Fact]
    public void Probe_Returns_Same_Value()
    {
        // Probe should be 'transparent' to the evaluation
        var (result, _) = Run("probe 123");

        Assert.Equal(123, Assert.IsType<Integer>(result).Number);
    }

    [Fact]
    public void Type_Question_Returns_Correct_Type_Word()
    {
        var (res1, _) = Run("type? 10");
        var (res2, _) = Run("type? [1 2]");
        var (res3, _) = Run("type? first ['hello]");

        Assert.Equal("integer!", Assert.IsType<Word>(res1).Name);
        Assert.Equal("block!", Assert.IsType<Word>(res2).Name);
        Assert.Equal("lit-word!", Assert.IsType<Word>(res3).Name);
    }

    [Fact]
    public void Print_Formats_Blocks_And_Strings_For_Humans()
    {
        var code = @"
            name: ""Alice""
            print [""Hello"" name]
        ";

        var (_, output) = RunWithOutput(code);

        // Should NOT have brackets or quotes
        Assert.Equal("Hello Alice", output);
    }

    [Fact]
    public void Series_Operations_Work()
    {
        var code = @"
            b: [10]
            append b 20
            first b
        ";
        var (res1, _) = Run(code);
        Assert.Equal(10, Assert.IsType<Integer>(res1).Number);

        var (res2, _) = Run("last [10 20 30]");
        Assert.Equal(30, Assert.IsType<Integer>(res2).Number);
    }

    [Fact]
    public void Path_Triggers_Refinement_Logic()
    {
        // We'll use a mock native to check if the refinement was received
        var ctx = Runtime.CreateGlobalContext();
        bool flagSet = false;

        ctx.Set("test-ref", new Native((args, refs, _, _, _) =>
        {
            flagSet = refs.Contains("flag");
            return new Word("none");
        }, 0));

        var interpreter = new Interpreter();

        // Execute call with refinement
        interpreter.Evaluate(new Loader().Load(new Lexer("test-ref/flag").Tokenize()), ctx);
        Assert.True(flagSet);

        // Execute call WITHOUT refinement
        flagSet = true; // Reset
        interpreter.Evaluate(new Loader().Load(new Lexer("test-ref").Tokenize()), ctx);
        Assert.False(flagSet);
    }

    [Fact]
    public void Reduce_Evaluates_Mixed_Block_Contents()
    {
        // Testing math, strings, and words all living together
        var code = @"
            val: 10
            reduce [""Result:"" add 2 2 :val]
        ";
        var (result, _) = Run(code);

        var block = Assert.IsType<Block>(result);
        Assert.Equal(3, block.Children.Count);
        
        // Check results: "Result:", 4, 10
        Assert.Equal("Result:", ((Text)block.Children[0]).Content);
        Assert.Equal(4, ((Integer)block.Children[1]).Number);
        Assert.Equal(10, ((Integer)block.Children[2]).Number);
    }

    [Fact]
    public void Reduce_Handles_Paths_And_Interop()
    {
        // Testing that paths are resolved inside the block during reduction
        var code = @"
            now: get-static ""System.DateTime"" ""Now""
            results: reduce [now/Year]
            results/1
        ";
        var (result, _) = Run(code);

        var year = Assert.IsType<Integer>(result);
        Assert.Equal(DateTime.Now.Year, (int)year.Number);
    }

    [Fact]
    public void Reduce_Returns_Empty_Block_For_Empty_Input()
    {
        var (result, _) = Run("reduce []");
        var block = Assert.IsType<Block>(result);
        Assert.Empty(block.Children);
    }

    [Fact]
    public void Paren_Evaluates_And_Returns_Last_Value()
    {
        // A paren should behave like a mini-block that executes immediately
        var (result, _) = Run("(add 10 20)");
        Assert.Equal(30, Assert.IsType<Integer>(result).Number);
    }

    [Fact]
    public void Paren_Supports_Deep_Nesting()
    {
        // Testing the recursive nature of the Loader and Interpreter
        var (result, _) = Run("(add 1 (multiply 2 (add 3 4)))");
        // 1 + (2 * (3 + 4)) = 1 + (2 * 7) = 15
        Assert.Equal(15, Assert.IsType<Integer>(result).Number);
    }

    [Fact]
    public void Paren_Works_As_Native_Argument()
    {
        // Ensure natives receive the evaluated result of the paren, not the paren itself
        var code = @"
            x: 0
            if (greater? 10 5) [x: 1]
            x
        ";
        var (result, _) = Run(code);
        Assert.Equal(1, Assert.IsType<Integer>(result).Number);
    }

    [Fact]
    public void Paren_Resolves_Words_From_Context()
    {
        var code = @"
            base: 10
            modifier: 5
            (add base modifier)
        ";
        var (result, _) = Run(code);
        Assert.Equal(15, Assert.IsType<Integer>(result).Number);
    }

    [Fact]
    public void Empty_Paren_Returns_None()
    {
        var (result, _) = Run("()");
        // Assuming your Evaluate returns Word("none") for empty blocks
        Assert.Equal("none", Assert.IsType<Word>(result).Name);
    }

    [Fact]
    public void Join_Concatenates_Two_Values()
    {
        var (result, _) = Run(@"join ""Age: "" 25");
        Assert.Equal("Age: 25", ((Text)result).Content);
    }

    [Fact]
    public void Rejoin_Reduces_And_Joins_Block()
    {
        // Testing that rejoin handles math inside the block
        var (result, _) = Run(@"rejoin [""2 + 2 = "" (add 2 2)]");
        Assert.Equal("2 + 2 = 4", ((Text)result).Content);
    }

    [Fact]
    public void Rejoin_Handles_Words_And_Strings()
    {
        var code = @"
            name: ""Ragnar""
            rejoin [""Hello, "" name ""!""]
        ";
        var (result, _) = Run(code);
        Assert.Equal("Hello, Ragnar!", ((Text)result).Content);
    }

    [Fact]
    public void Does_Creates_A_Function_Without_Arguments()
    {
        var code = @"
            say-hi: does [ ""hi"" ]
            say-hi
        ";
        var (result, _) = Run(code);
        Assert.Equal("hi", ((Text)result).Content);
    }
}