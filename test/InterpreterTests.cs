
namespace rebelly.tests;

public class InterpreterTests
{
    /// <summary>
    /// Helper to execute a snippet of Rebelly code and return the final 
    /// evaluation result and the state of the context.
    /// </summary>
    private (Value Result, Context Context) Run(string code)
    {
        var lexer = new Lexer(code);
        var tokens = lexer.Tokenize();
        var loader = new Loader();
        var root = loader.Load(tokens);
        
        var ctx = Runtime.CreateGlobalContext();
        var interpreter = new Interpreter();
        
        var result = interpreter.Evaluate(root, ctx);
        return (result, ctx);
    }

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
        
        var lexer = new Lexer(code);
        var tokens = lexer.Tokenize();
        var root = new Loader().Load(tokens);
        var ctx = Runtime.CreateGlobalContext();
        var interpreter = new Interpreter();

        using var sw = new StringWriter();
        ctx.Output = sw; // Redirect output to our StringWriter

        interpreter.Evaluate(root, ctx);

        var output = sw.ToString().Trim();
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
        
        ctx.Set("test-ref", new Native((args, refs, _, _) => {
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
}