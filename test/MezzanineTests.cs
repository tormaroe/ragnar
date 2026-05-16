
using Xunit;
using Ragnar;

namespace Ragnar.Tests;

public class MezzanineTests : TestBase
{
    [Fact]
    public void Test_Enumerate_Word_Block()
    {
        string script = @"
            vars: call-static ""System.Environment"" ""GetEnvironmentVariables"" []
            result: []
            enumerate vars item [
                append result item/key
            ]
            length? result
        ";

        var result = (Integer)Run(script).Result;
        Assert.True(result.Number > 0);
    }

    [Fact]
    public void Test_List_Env()
    {
        string script = @"
            env: list-env
            length? env
        ";

        var result = (Integer)Run(script).Result;
        Assert.True(result.Number > 0);
        Assert.True(result.Number % 2 == 0); // Name-value pairs
    }
}
