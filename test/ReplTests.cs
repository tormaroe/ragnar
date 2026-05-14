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
}
