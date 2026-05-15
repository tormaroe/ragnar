using Xunit;
using Ragnar;
using System.Collections.Generic;

namespace Ragnar.Tests;

public class ObjectTests : TestBase
{
    [Fact]
    public void Make_Object_Creates_Bindings()
    {
        var code = @"
            obj: make object! [
                a: 10
                b: 20
                sum: a + b
            ]
            obj/sum
        ";
        var (result, _) = Run(code);
        Assert.Equal(30, ((Integer)result).Number);
    }

    [Fact]
    public void Object_Self_Works()
    {
        var code = @"
            obj: make object! [
                name: ""Ragnar""
                greet: func [] [ rejoin [""Hello, "" self/name] ]
            ]
            obj/greet
        ";
        var (result, _) = Run(code);
        Assert.Equal("Hello, Ragnar", result.ToUserString());
    }

    [Fact]
    public void Set_Object_Property_Via_Path()
    {
        var code = @"
            obj: make object! [ a: 1 ]
            obj/a: 100
            obj/a
        ";
        var (result, _) = Run(code);
        Assert.Equal(100, ((Integer)result).Number);
    }

    [Fact]
    public void In_And_Get_Work()
    {
        var code = @"
            obj: make object! [ x: 42 ]
            word: in obj 'x
            get word
        ";
        var (result, _) = Run(code);
        Assert.Equal(42, ((Integer)result).Number);
    }

    [Fact]
    public void First_Object_Returns_Keys()
    {
        var code = @"
            obj: make object! [ a: 1 b: 2 ]
            keys: first obj
            length? keys
        ";
        var (result, _) = Run(code);
        // self, a, b = 3 keys
        Assert.Equal(3, ((Integer)result).Number);
    }

    [Fact]
    public void Probe_Object_Example_Works()
    {
        var code = @"
            probe-object: func [object][
                res: """"
                foreach word next first object [
                    res: rejoin [res word "":"" get in object word "" ""]
                ]
                res
            ]
            obj: make object! [ name: ""Alice"" age: 30 ]
            probe-object obj
        ";
        var (result, _) = Run(code);
        var text = (Text)result;
        Assert.Contains("name:Alice", text.Content);
        Assert.Contains("age:30", text.Content);
    }

    [Fact]
    public void Nested_Object_Access_Works()
    {
        var code = @"
            person: make object! [
                address: make object! [
                    city: ""Oslo""
                ]
            ]
            person/address/city
        ";
        var (result, _) = Run(code);
        Assert.Equal("Oslo", ((Text)result).Content);
    }
}
