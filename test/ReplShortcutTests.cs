
namespace Ragnar.Tests;

public class ReplShortcutTests : TestBase
{
    [Fact]
    public void It_Shortcut_Returns_Last_Result()
    {
        // 1. Evaluate something
        // 2. Use 'it' in the next expression
        var code = @"
            10
            add it 5
        ";
        var (result, _) = Run(code);
        Assert.Equal(15, Assert.IsType<Integer>(result).Number);
    }

    [Fact]
    public void It_Updates_Continuously_In_Block()
    {
        var code = @"
            1
            add it 1
            add it 1
            add it 1
        ";
        var (result, _) = Run(code);
        Assert.Equal(4, Assert.IsType<Integer>(result).Number);
    }

    [Fact]
    public void Users_Specific_Example_Works()
    {
        // ""1 add it 2 equal? 3 it""
        // Assume initial 'it' was say 1.
        // 1 -> last is 1.
        // add it 2 -> add 1 2 -> 3. last is 3.
        // equal? 3 it -> equal? 3 3 -> true.
        
        // We need to set an initial LastResult for this to work as expected in one block
        var ctx = Runtime.CreateGlobalContext();
        ctx.LastResult = new Integer(1);
        
        var interpreter = new Interpreter();
        var tokens = new Lexer("1 add it 2 equal? 3 it").Tokenize();
        var block = new Loader().Load(tokens);
        var result = interpreter.Evaluate(block, ctx);
        
        Assert.True(Assert.IsType<Logic>(result).Condition);
    }

    [Fact]
    public void It_Shortcut_In_Nested_Block()
    {
        var code = @"
            42
            if true [ it ]
        ";
        var (result, _) = Run(code);
        Assert.Equal(42, Assert.IsType<Integer>(result).Number);
    }

    [Fact]
    public void User_Can_Set_It_To_Value()
    {
        var code = @"
            it: 100
            add it 5
        ";
        var (result, _) = Run(code);
        Assert.Equal(105, Assert.IsType<Integer>(result).Number);
    }

    [Fact]
    public void It_Always_Reflects_Last_Result_Even_After_Explicit_Set()
    {
        var code = @"
            it: 100
            5 + 5
            add it 5
        ";
        // it: 100 -> LastResult = 100
        // 5 + 5 -> LastResult = 10
        // add it 5 -> 10 + 5 = 15
        var (result, _) = Run(code);
        Assert.Equal(15, Assert.IsType<Integer>(result).Number);
    }

    [Fact]
    public void It_Tracks_Results_Inside_Loop()
    {
        var code = @"
            foreach it [1 2 3] [
                ; 'it' starts as loop element
                ; but then we do an expression
                5 + 5
                ; now 'it' should be 10
            ]
        ";
        var (result, _) = Run(code);
        // The last value of the loop is the result of the last expression in the last iteration
        Assert.Equal(10, Assert.IsType<Integer>(result).Number);
    }
}
