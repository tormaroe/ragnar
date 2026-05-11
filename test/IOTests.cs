namespace Ragnar.Tests;

public class IOTests
{
    private (Value Result, Context Context) Run(string code)
    {
        var lexer = new Lexer(code);
        var tokens = lexer.Tokenize();
        var loader = new Loader();
        var root = loader.Load(tokens);

        var ctx = Runtime.CreateGlobalContext();
        var interpreter = new Interpreter();

        var result = interpreter.Evaluate(root, ctx);
        return (result, ctx);
    }

    [Fact]
    public void File_IO_Roundtrip_Works()
    {
        string tempFile = System.IO.Path.GetTempFileName();
        var code = $@"
        target: %{tempFile.Replace("\\", "/")}
        write :target ""Hello from Ragnar!""
        read :target
    ";

        var (result, _) = Run(code);

        var textResult = Assert.IsType<Text>(result);
        Assert.Equal("Hello from Ragnar!", textResult.Content);

        if (System.IO.File.Exists(tempFile)) System.IO.File.Delete(tempFile);
    }

    [Fact]
    public void Read_Lines_Returns_Block_Of_Strings()
    {
        string tempFile = System.IO.Path.GetTempFileName();
        // Create a file with 3 lines
        System.IO.File.WriteAllLines(tempFile, ["one", "two", "three"]);

        var code = $@"
        target: %{tempFile.Replace("\\", "/")}
        data: read/lines :target
        length? :data
    ";

        var (result, _) = Run(code);
        Assert.Equal(3, Assert.IsType<Integer>(result).Number);

        if (System.IO.File.Exists(tempFile)) System.IO.File.Delete(tempFile);
    }

    [Fact]
    public void Write_Append_Does_Not_Overwrite()
    {
        string tempFile = System.IO.Path.GetTempFileName();
        var code = $@"
        file: %{tempFile.Replace("\\", "/")}
        write :file ""A""
        write/append :file ""B""
        read :file
    ";

        var (result, _) = Run(code);
        Assert.Equal("AB", Assert.IsType<Text>(result).Content);

        if (System.IO.File.Exists(tempFile)) System.IO.File.Delete(tempFile);
    }

    [Fact]
    public void Foreach_Works_With_File_Lines()
    {
        // Simulate reading a file into lines
        string tempFile = System.IO.Path.GetTempFileName();
        System.IO.File.WriteAllLines(tempFile, ["Alice", "Bob"]);

        var code = $@"
            len: 0
            names: read/lines %{tempFile.Replace("\\", "/")}
            foreach name names [
                len: add len length? name
            ]
            len
        ";

        var (result, _) = Run(code);
        Assert.Equal(8, Assert.IsType<Integer>(result).Number); // "Alice" has 5 letters, "Bob" has 3 letters

        if (System.IO.File.Exists(tempFile)) System.IO.File.Delete(tempFile);
    }
}