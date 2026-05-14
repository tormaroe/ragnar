namespace Ragnar.Tests;

public class HelpTests : TestBase
{
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