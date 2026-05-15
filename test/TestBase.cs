namespace Ragnar.Tests;

public abstract class TestBase
{
    protected (Value Result, Context Context) Run(string code)
    {
        var ctx = Runtime.CreateGlobalContext();
        var interpreter = new Interpreter();

        // Load Mezzanine
        var mezzTokens = new Lexer(Mezzanine.SOURCE).Tokenize();
        var mezzRoot = new Loader().Load(mezzTokens);
        interpreter.Evaluate(mezzRoot, ctx);

        var lexer = new Lexer(code);
        var tokens = lexer.Tokenize();
        var loader = new Loader();
        var root = loader.Load(tokens);

        var result = interpreter.Evaluate(root, ctx);
        return (result, ctx);
    }

    protected (Value Result, string Output) RunWithOutput(string code)
    {
        using var sw = new System.IO.StringWriter();
        
        var ctx = Runtime.CreateGlobalContext();
        ctx.Output = sw; 
        var interpreter = new Interpreter();

        // Load Mezzanine
        var mezzTokens = new Lexer(Mezzanine.SOURCE).Tokenize();
        var mezzRoot = new Loader().Load(mezzTokens);
        interpreter.Evaluate(mezzRoot, ctx);

        var lexer = new Lexer(code);
        var tokens = lexer.Tokenize();
        var root = new Loader().Load(tokens);
        
        var result = interpreter.Evaluate(root, ctx);
        
        return (result, sw.ToString().Trim());
    }
}
