namespace Ragnar.Tests;

public class HelpTests : TestBase
{
    [Fact]
    public void Help_Displays_Native_Info()
    {
        // Test with direct word (unquoted)
        var (_, output) = RunWithOutput("help add");
        
        Assert.Contains("WORD: add", output);
        Assert.Contains("TYPE:  Native Function", output);
        Assert.Contains("TITLE: Returns the sum of two values.", output);
        Assert.Contains("ARITY: 2", output);

        // Test with quoted word (lit-word)
        var (_, output2) = RunWithOutput("help 'add");
        Assert.Contains("WORD: add", output2);
    }

    [Fact]
    public void Help_Displays_User_Function_Info()
    {
        var code = @"
            square: func [""Returns the square of a number."" n] [n * n]
            help square
        ";
        var (_, output) = RunWithOutput(code);
        
        Assert.Contains("WORD: square", output);
        Assert.Contains("TYPE:  User-Defined Function", output);
        Assert.Contains("TITLE: Returns the square of a number.", output);
        Assert.Contains("ARGS:  [ n ]", output);
    }

    [Fact]
    public void Help_Handles_Undefined_Words_Gracefully()
    {
        var (_, output) = RunWithOutput("help 'ghost");
        
        Assert.Contains("Word 'ghost' is not defined", output);
    }

    [Fact]
    public void What_Lists_Functions_With_Titles()
    {
        var (_, output) = RunWithOutput("what");
        
        // Check for some common functions and their titles
        Assert.Contains("add             Returns the sum of two values.", output);
        Assert.Contains("print           Prints a value to the output.", output);
    }

    [Fact]
    public void Mezzanine_Functions_Have_Titles()
    {
        var (_, output) = RunWithOutput("help 'max");
        Assert.Contains("TITLE: Returns the greater of two values.", output);
        
        var (_, output2) = RunWithOutput("help 'does");
        Assert.Contains("TITLE: Defines a function with no arguments.", output2);
    }

    [Fact]
    public void Infix_Operators_Have_Titles()
    {
        var (_, output) = RunWithOutput("help '+");
        Assert.Contains("TITLE: Returns the sum of two values.", output);

        var (_, output2) = RunWithOutput("help '=");
        Assert.Contains("TITLE: Returns true if the values are equal.", output2);
    }
}