
namespace Ragnar.Tests;

public class InteropTests : TestBase
{
    [Fact]
    public void GetType_Resolves_Valid_DotNet_Type()
    {
        var (result, _) = Run("get-type \"System.Int32\"");

        var dnv = Assert.IsType<DotNetValue>(result);
        Assert.Equal(typeof(int), dnv.Instance);
    }

    [Fact]
    public void New_Instantiates_Class_With_Multiple_Args()
    {
        // System.Version is a great test because it strictly requires Int32
        var code = @"
            ver-type: get-type ""System.Version""
            my-ver: new :ver-type [1 2 3]
            get-prop my-ver ""Minor""
        ";

        var (result, _) = Run(code);

        var intResult = Assert.IsType<Integer>(result);
        Assert.Equal(2, intResult.Number);
    }

    [Fact]
    public void New_Instantiates_Struct_Like_DateTime()
    {
        var code = @"
            dt-type: get-type ""System.DateTime""
            my-date: new :dt-type [2026 12 25]
            get-prop my-date ""Month""
        ";

        var (result, _) = Run(code);

        var intResult = Assert.IsType<Integer>(result);
        Assert.Equal(12, intResult.Number);
    }

    [Fact]
    public void GetProp_Can_Retrieve_Strings()
    {
        // Testing that the bridge converts .NET strings back to Ragnar Text
        var code = @"
            ex-type: get-type ""System.Exception""
            msg: ""Something went wrong""
            my-ex: new :ex-type [:msg]
            get-prop my-ex ""Message""
        ";

        var (result, _) = Run(code);

        var textResult = Assert.IsType<Text>(result);
        Assert.Equal("Something went wrong", textResult.Content);
    }

    [Fact]
    public void Interop_Throws_On_Missing_Type()
    {
        Assert.Throws<Exception>(() => Run("get-type \"NonExistent.Class\""));
    }

    [Fact]
    public void CallMethod_Invokes_And_Handles_Chaining()
    {
        // We create a StringBuilder, append a string, and then call ToString
        var code = @"
            sb-type: get-type ""System.Text.StringBuilder""
            sb: new :sb-type []
            
            ; Append returns the StringBuilder instance (DotNetValue)
            call-method sb ""Append"" [""Hello ""]
            
            ; We can pass variables as arguments too
            tail: ""Ragnar!""
            call-method sb ""Append"" [:tail]
            
            ; Final call to get the result
            res: call-method sb ""ToString"" []
        ";

        var (result, ctx) = Run(code);

        // The last expression was call-method ... ""ToString"" []
        var textResult = Assert.IsType<Text>(result);
        Assert.Equal("Hello Ragnar!", textResult.Content);
    }

    [Fact]
    public void CallMethod_Supports_Math_On_Objects()
    {
        // Test DateTime.AddDays (requires a double/Decimal)
        var code = @"
            dt-type: get-type ""System.DateTime""
            my-date: new :dt-type [2026 5 9]
            
            ; Add 10.0 days (decimal)
            new-date: call-method my-date ""AddDays"" [10.0]
            get-prop new-date ""Day""
        ";

        var (result, _) = Run(code);

        var intResult = Assert.IsType<Integer>(result);
        Assert.Equal(19, intResult.Number);
    }

    [Fact]
    public void SetProp_Updates_DotNet_Object_State()
    {
        var code = @"
            sb-type: get-type ""System.Text.StringBuilder""
            sb: new :sb-type []
            
            ; Set the Capacity property (requires an Int32)
            new-cap: 128
            set-prop sb ""Capacity"" :new-cap
            
            ; Verify by reading it back
            get-prop sb ""Capacity""
        ";

        var (result, _) = Run(code);

        var intResult = Assert.IsType<Integer>(result);
        Assert.Equal(128, intResult.Number);
    }

    [Fact]
    public void SetProp_Handles_Strings_Correctly()
    {
        var code = @"
            ex-type: get-type ""System.Exception""
            my-ex: new :ex-type [""Original Error""]
            
            set-prop my-ex ""Source"" ""RagnarEngine""
            get-prop my-ex ""Source""
        ";

        var (result, _) = Run(code);

        var textResult = Assert.IsType<Text>(result);
        Assert.Equal("RagnarEngine", textResult.Content);
    }

    [Fact]
    public void Static_Property_Access_Returns_Native_Type()
    {
        var code = @"
            now: get-static ""System.DateTime"" ""Now""
            get-prop :now ""Year""
        ";
        var (result, _) = Run(code);

        // We no longer expect DotNetValue! We expect a native Ragnar Integer.
        var ragnarInt = Assert.IsType<Integer>(result);
        Assert.Equal(DateTime.Now.Year, ragnarInt.Number);
    }

    [Fact]
    public void Static_Method_Call_Works()
    {
        // Math.Abs(-100) should return 100
        // We pass -100 as a Ragnar Integer, and it should return a Ragnar Integer.
        var code = @"
            call-static ""System.Math"" ""Abs"" [ -100 ]
        ";

        var (result, _) = Run(code);

        // Verify the return value was coerced from a .NET int back to a Ragnar Integer
        var ragnarInt = Assert.IsType<Integer>(result);
        Assert.Equal(100, ragnarInt.Number);
    }

    [Fact]
    public void Static_String_Method_Works()
    {
        // string.Concat("Ragnar", "!", "!")
        var code = @"
            call-static ""System.String"" ""Concat"" [ ""Ragnar"" ""!"" ]
        ";

        var (result, _) = Run(code);

        var ragnarText = Assert.IsType<Text>(result);
        Assert.Equal("Ragnar!", ragnarText.Content);
    }

    [Fact]
    public void Static_Method_With_Multiple_Args_Works()
    {
        // Path.Combine("C:/", "Ragnar")
        var code = @"
            call-static ""System.IO.Path"" ""Combine"" [ ""C:/"" ""Ragnar"" ]
        ";

        var (result, _) = Run(code);

        var text = Assert.IsType<Text>(result);
        // Path.Combine is OS sensitive, so we check for the name
        Assert.Contains("Ragnar", text.Content);
    }

    [Fact]
    public void Get_Env_Retrieves_Environment_Variable()
    {
        Environment.SetEnvironmentVariable("RAGNAR_TEST", "rocks");
        try
        {
            var (result, _) = Run("get-env \"RAGNAR_TEST\"");
            Assert.Equal("rocks", ((Text)result).Content);
        }
        finally
        {
            Environment.SetEnvironmentVariable("RAGNAR_TEST", null);
        }
    }
}