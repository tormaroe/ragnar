using Xunit;
using Ragnar;

namespace Ragnar.Tests;

public class KoanTests : TestBase
{
    [Fact]
    public void Test_Substitute_Blank()
    {
        string script = @"
            do %koans/KoanMode.r
            expr1: [equal? true __]
            res1: substitute-blank expr1 true
            
            expr2: first [ (__ = true) ]
            res2: substitute-blank expr2 false
            
            expr3: to-record [a: __ b: 2]
            res3: substitute-blank expr3 5
            
            reduce [res1 res2 res3]
        ";
        var (result, _) = Run(script);
        Assert.Equal("[ [ equal? true true ] (false = true) #( a: 5 b: 2 ) ]", result.ToString());
    }
}
