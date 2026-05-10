namespace Ragnar.Tests;

public class HelpTests
{
    private (Value Result, string Output) RunWithOutput(string code)
    {
        // 1. Setup the private buffer
        using var sw = new StringWriter();
        
        // 2. Standard pipeline
        var lexer = new Lexer(code);
        var tokens = lexer.Tokenize();
        var root = new Loader().Load(tokens);
        
        // 3. Inject the StringWriter into the Context
        var ctx = Runtime.CreateGlobalContext();
        ctx.Output = sw; 
        
        var interpreter = new Interpreter();
        
        // 4. Evaluate
        var result = interpreter.Evaluate(root, ctx);
        
        // 5. Return both the functional result and the text output
        return (result, sw.ToString().Trim());
    }

    [Fact]
    public void Help_Displays_Native_Info()
    {
        var (_, output) = RunWithOutput("help 'add");
        
        Assert.Contains("WORD: add", output);
        Assert.Contains("TYPE:  Native Function", output);
        Assert.Contains("ARITY: 2", output);
    }

    [Fact]
    public void Help_Displays_User_Function_Info()
    {
        var code = @"
            square: func [n] [mul n n]
            help 'square
        ";
        var (_, output) = RunWithOutput(code);
        
        Assert.Contains("WORD: square", output);
        Assert.Contains("TYPE:  User-Defined Function", output);
        Assert.Contains("ARGS:  [ n ]", output);
    }

    [Fact]
    public void Help_Handles_Undefined_Words_Gracefully()
    {
        var (_, output) = RunWithOutput("help 'ghost");
        
        Assert.Contains("Word 'ghost' is not defined", output);
    }
}