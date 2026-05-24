using Xunit;
using Ragnar;
using System;

namespace Ragnar.Tests;

public class JsonLibraryTests : TestBase
{
    [Fact]
    public void Record_TypeCheck_And_BlockRestrictions_Work()
    {
        var code = @"
            r: to-record [a: 1 b: 2]
            reduce [type? r block? r record? r]
        ";
        var result = (Block)Run(code).Result;
        Assert.Equal("record!", ((Word)result.Children[0]).Name);
        Assert.False(((Logic)result.Children[1]).Condition);
        Assert.True(((Logic)result.Children[2]).Condition);
    }

    [Fact]
    public void Record_EvenElementConstraint_Works()
    {
        Assert.ThrowsAny<Exception>(() => Run("to-record [a: 1 b]"));
    }

    [Fact]
    public void Record_BlockOperations_Work()
    {
        var code = @"
            r: to-record [a: 1 b: 2]
            append r 3
            append r 4
            reduce [length? r pick r 1 pick r 4 select r 'b]
        ";
        var result = (Block)Run(code).Result;
        Assert.Equal(6, ((Integer)result.Children[0]).Number);
        Assert.Equal("a:", ((SetWord)result.Children[1]).ToString());
        Assert.Equal(2, ((Integer)result.Children[2]).Number);
        Assert.Equal(2, ((Integer)result.Children[3]).Number);
    }

    [Fact]
    public void SeriesOperations_NavigationAndMutation_Work()
    {
        var code = @"
            b: [1 2 3]
            t: copy ""abc""
            
            ; back, head, tail
            b2: next b
            b3: back b2
            
            t2: next t
            t3: back t2
            
            ; take, remove, clear
            val-b: take b2
            val-t: take t2
            
            remove b ; removes first
            clear t3 ; clears everything
            
            reduce [
                index? b3
                index? t3
                val-b
                val-t
                b
                t
            ]
        ";
        var result = (Block)Run(code).Result;
        Assert.Equal(1, ((Integer)result.Children[0]).Number);
        Assert.Equal(1, ((Integer)result.Children[1]).Number);
        Assert.Equal(2, ((Integer)result.Children[2]).Number);
        Assert.Equal('b', ((Character)result.Children[3]).CharValue);
        Assert.Equal("[ 3 ]", result.Children[4].ToString());
        Assert.Equal("\"\"", result.Children[5].ToString());
    }

    [Fact]
    public void Json_Stringify_Primitives_Works()
    {
        var code = @"
            json: do %lib/json.r
            reduce [
                json/stringify 123
                json/stringify 45.67
                json/stringify ""Hello\\World""
                json/stringify true
                json/stringify false
                json/stringify none
            ]
        ";
        var result = (Block)Run(code).Result;
        Assert.Equal("123", ((Text)result.Children[0]).Content);
        Assert.Equal("45.67", ((Text)result.Children[1]).Content);
        Assert.Equal("\"Hello\\\\World\"", ((Text)result.Children[2]).Content);
        Assert.Equal("true", ((Text)result.Children[3]).Content);
        Assert.Equal("false", ((Text)result.Children[4]).Content);
        Assert.Equal("null", ((Text)result.Children[5]).Content);
    }

    [Fact]
    public void Json_Stringify_ControlChars_Works()
    {
        var code = @"
            json: do %lib/json.r
            json/stringify rejoin [""Line1"" to-char 10 ""Line2"" to-char 9 ""End""]
        ";
        var result = (Text)Run(code).Result;
        Assert.Equal("\"Line1\\nLine2\\tEnd\"", result.Content);
    }

    [Fact]
    public void Json_Stringify_ComplexStructures_Works()
    {
        var code = @"
            json: do %lib/json.r
            data: reduce [
                10
                to-record [name ""Alice"" age 30]
                [true false]
            ]
            json/stringify data
        ";
        var result = (Text)Run(code).Result;
        Assert.Equal("[10,{\"name\":\"Alice\",\"age\":30},[true,false]]", result.Content);
    }

    [Fact]
    public void Json_StringifyPretty_Works()
    {
        var code = @"
            json: do %lib/json.r
            data: to-record [name ""Alice"" age 30]
            json/stringify/pretty data
        ";
        var result = (Text)Run(code).Result;
        var expected = "{\n    \"name\": \"Alice\",\n    \"age\": 30\n}";
        Assert.Equal(expected.Replace("\n", Environment.NewLine), result.Content.Replace("\r\n", "\n").Replace("\n", Environment.NewLine));
    }

    [Fact]
    public void Json_Parse_Primitives_Works()
    {
        var code = @"
            json: do %lib/json.r
            reduce [
                json/parse ""123""
                json/parse ""-45.67""
                json/parse ""\""Hello\""""
                json/parse ""true""
                json/parse ""false""
                json/parse ""null""
            ]
        ";
        var result = (Block)Run(code).Result;
        Assert.Equal(123, ((Integer)result.Children[0]).Number);
        Assert.Equal(-45.67, ((Decimal)result.Children[1]).Number);
        Assert.Equal("Hello", ((Text)result.Children[2]).Content);
        Assert.True(((Logic)result.Children[3]).Condition);
        Assert.False(((Logic)result.Children[4]).Condition);
        Assert.Equal("none", ((Word)result.Children[5]).Name);
    }

    [Fact]
    public void Json_Parse_UnicodeEscape_Works()
    {
        var code = @"
            json: do %lib/json.r
            json/parse ""\""Hello\\u0020World\""""
        ";
        var result = (Text)Run(code).Result;
        Assert.Equal("Hello World", result.Content);
    }

    [Fact]
    public void Json_Parse_ComplexStructures_Works()
    {
        var code = @"
            json: do %lib/json.r
            res: json/parse ""{\""name\"": \""Alice\"", \""age\"": 30, \""favorites\"": [\""blue\"", \""pizza\""]}""
            reduce [
                type? res
                select res 'name
                select res 'age
                select res 'favorites
            ]
        ";
        var result = (Block)Run(code).Result;
        Assert.Equal("record!", ((Word)result.Children[0]).Name);
        Assert.Equal("Alice", ((Text)result.Children[1]).Content);
        Assert.Equal(30, ((Integer)result.Children[2]).Number);
        
        var favs = (Block)result.Children[3];
        Assert.Equal("blue", ((Text)favs.Children[0]).Content);
        Assert.Equal("pizza", ((Text)favs.Children[1]).Content);
    }

    [Fact]
    public void Json_Parse_InvalidJson_ThrowsError()
    {
        var code = @"
            json: do %lib/json.r
            attempt [json/parse ""{\""invalid\""}"" 42]
        ";
        var result = Run(code).Result;
        Assert.Equal("none", result.ToUserString());
    }
}
