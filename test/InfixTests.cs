using Xunit;
using Ragnar;

namespace Ragnar.Tests;

public class InfixTests
{
    private readonly Interpreter _interpreter = new();
    private readonly Context _context = Runtime.CreateGlobalContext();

    private Value Eval(string code)
    {
        var tokens = new Lexer(code).Tokenize();
        var block = new Loader().Load(tokens);
        return _interpreter.Evaluate(block, _context);
    }

    [Theory]
    [InlineData("1 + 2", 3)]
    [InlineData("10 - 4", 6)]
    [InlineData("3 * 4", 12)]
    [InlineData("12 / 3", 4)]
    [InlineData("10 // 3", 1)]
    [InlineData("11 // 4", 3)]
    public void MathInfix_BasicOperations(string code, object expected)
    {
        var result = Eval(code);
        if (expected is int i)
            Assert.Equal((long)i, ((Integer)result).Number);
        else if (expected is double d)
            Assert.Equal(d, ((Decimal)result).Number);
        else if (expected is long l)
            Assert.Equal(l, ((Integer)result).Number);
    }

    [Theory]
    [InlineData("1 + 2 * 3", 9)] // Left-associative: (1 + 2) * 3
    [InlineData("10 - 2 + 3", 11)] // (10 - 2) + 3
    [InlineData("2 * 3 + 4", 10)] // (2 * 3) + 4
    public void MathInfix_LeftAssociativity(string code, long expected)
    {
        var result = (Integer)Eval(code);
        Assert.Equal(expected, result.Number);
    }

    [Theory]
    [InlineData("1 < 2", true)]
    [InlineData("1 > 2", false)]
    [InlineData("1 = 1", true)]
    [InlineData("1 == 1", true)]
    [InlineData("1 <> 2", true)]
    [InlineData("1 != 2", true)]
    [InlineData("2 >= 2", true)]
    [InlineData("2 <= 1", false)]
    public void ComparisonInfix_BasicOperations(string code, bool expected)
    {
        var result = (Logic)Eval(code);
        Assert.Equal(expected, result.Condition);
    }

    [Theory]
    [InlineData("add 1 2 * 3", 7)] // Rebol: add 1 (2 * 3) = 7
    [InlineData("(add 1 2) * 3", 9)] 
    [InlineData("1 + add 2 3", 6)] // 1 + (add 2 3) = 6
    [InlineData("remainder 10 3", 1)]
    public void Infix_And_Prefix_Mixing(string code, long expected)
    {
        var result = (Integer)Eval(code);
        Assert.Equal(expected, result.Number);
    }
}
