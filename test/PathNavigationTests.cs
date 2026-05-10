namespace Ragnar.Tests;

public class PathNavigationTests
{
    private (Value Result, string Output) Run(string code)
    {
        var lexer = new Lexer(code);
        var tokens = lexer.Tokenize();
        var root = new Loader().Load(tokens);
        var ctx = Runtime.CreateGlobalContext();
        var interpreter = new Interpreter();
        return (interpreter.Evaluate(root, ctx), "");
    }

    [Fact]
    public void Path_Navigates_Block_Index()
    {
        // Rebol/Ragnar is 1-indexed
        var code = "data: [10 20 30] data/2";
        var (result, _) = Run(code);

        var val = Assert.IsType<Integer>(result);
        Assert.Equal(20, val.Number);
    }

    [Fact]
    public void Path_Navigates_DotNet_Instance()
    {
        // Using DateTime.Now.Year
        var code = @"
            now: get-static ""System.DateTime"" ""Now""
            now/Year
        ";
        var (result, _) = Run(code);

        var val = Assert.IsType<Integer>(result);
        Assert.Equal(DateTime.Now.Year, (int)val.Number);
    }

    [Fact]
    public void Path_Navigates_DotNet_Static_Member()
    {
        // Testing deep pathing into a static class
        var code = "System.Math/PI";
        var (result, _) = Run(code);

        var val = Assert.IsType<Decimal>(result);
        Assert.Equal(Math.PI, val.Number);
    }

    [Fact]
    public void Path_Navigates_Case_Insensitive()
    {
        // Our GetDotNetMember helper uses BindingFlags.IgnoreCase
        var code = @"
            now: get-static ""System.DateTime"" ""Now""
            now/year
        ";
        var (result, _) = Run(code);

        var val = Assert.IsType<Integer>(result);
        Assert.Equal(DateTime.Now.Year, (int)val.Number);
    }

    [Fact]
    public void Path_Returns_None_On_Invalid_Index()
    {
        // Assign to a word so that 'b/5' is parsed as a single Path token
        var code = "b: [1 2 3] b/5";
        var (result, _) = Run(code);

        var word = Assert.IsType<Word>(result);
        Assert.Equal("none", word.Name);
    }
}