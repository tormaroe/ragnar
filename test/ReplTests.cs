using Xunit;
using Ragnar;

namespace Ragnar.Tests;

public class ReplTests
{
    [Fact]
    public void AddHistory_ShouldAddUniqueNonEmptyLines()
    {
        var repl = new Repl();
        repl.AddHistory("1 + 1");
        repl.AddHistory("1 + 1"); // Duplicate
        repl.AddHistory("");      // Empty
        repl.AddHistory("2 + 2");

        Assert.Equal(2, repl._history.Count);
        Assert.Equal("1 + 1", repl._history[0]);
        Assert.Equal("2 + 2", repl._history[1]);
    }

    [Fact]
    public void ReplHighlighter_ShouldColorizeVariousSyntaxElementsWithoutErrors()
    {
        var ctx = Runtime.CreateGlobalContext();
        
        using (var sw = new System.IO.StringWriter())
        {
            var originalOut = Console.Out;
            Console.SetOut(sw);
            try
            {
                // Basic types
                ReplHighlighter.WriteColored("123 45.67", ctx);
                ReplHighlighter.WriteColored("\"string\" {brace string}", ctx);
                ReplHighlighter.WriteColored("; comment", ctx);
                ReplHighlighter.WriteColored("[ ] ( )", ctx);
                ReplHighlighter.WriteColored("true false none", ctx);
                
                // Words
                ReplHighlighter.WriteColored("word: :word 'word", ctx);
                ReplHighlighter.WriteColored("%file.txt", ctx);
                
                // Functions/Natives
                ReplHighlighter.WriteColored("print print/lines", ctx);
                
                // Incomplete strings/braces
                ReplHighlighter.WriteColored("\"incomplete", ctx);
                ReplHighlighter.WriteColored("{incomplete", ctx);
                ReplHighlighter.WriteColored("(#\"incomplete", ctx);
                
                // Verify the text content was correctly printed
                string output = sw.ToString();
                Assert.Contains("123 45.67", output);
                Assert.Contains("\"string\" {brace string}", output);
                Assert.Contains("; comment", output);
                Assert.Contains("print print/lines", output);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }
    }
}
