using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Ragnar.Tests;

public class ReplHistoryTests : IDisposable
{
    private readonly string _tempTestFile;

    public ReplHistoryTests()
    {
        _tempTestFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $".ragnar_history_test_{Guid.NewGuid()}");
        ReplHistoryManager.HistoryFilePath = _tempTestFile;
    }

    public void Dispose()
    {
        ReplHistoryManager.ResetForTesting();
        if (System.IO.File.Exists(_tempTestFile))
        {
            try { System.IO.File.Delete(_tempTestFile); } catch {}
        }
        if (System.IO.File.Exists(_tempTestFile + ".tmp"))
        {
            try { System.IO.File.Delete(_tempTestFile + ".tmp"); } catch {}
        }
    }

    [Fact]
    public void DefaultPath_ShouldEndWithRagnarHistory()
    {
        ReplHistoryManager.HistoryFilePath = null!; // Reset to default
        var path = ReplHistoryManager.HistoryFilePath;
        Assert.NotNull(path);
        Assert.EndsWith(".ragnar_history", path);
    }

    [Fact]
    public void LoadHistory_ShouldReturnEmpty_WhenFileDoesNotExist()
    {
        var lines = ReplHistoryManager.LoadHistory();
        Assert.Empty(lines);
    }

    [Fact]
    public void LoadHistory_ShouldFilterConsecutiveDuplicatesAndEmptyLines()
    {
        System.IO.File.WriteAllLines(_tempTestFile, new[]
        {
            "print \"hello\"",
            "print \"hello\"", // consecutive duplicate
            "   ",             // whitespace
            "1 + 1",
            "print \"hello\"", // non-consecutive duplicate (kept)
            "",                // empty
            "2 + 2"
        });

        var lines = ReplHistoryManager.LoadHistory();

        Assert.Equal(4, lines.Count);
        Assert.Equal("print \"hello\"", lines[0]);
        Assert.Equal("1 + 1", lines[1]);
        Assert.Equal("print \"hello\"", lines[2]);
        Assert.Equal("2 + 2", lines[3]);
    }

    [Fact]
    public void LoadHistory_ShouldPruneWhenExceedingMaxSize()
    {
        // Max entries is 1000. Write 1005 lines.
        var originalLines = Enumerable.Range(1, 1005).Select(i => $"command-{i}").ToList();
        System.IO.File.WriteAllLines(_tempTestFile, originalLines);

        var lines = ReplHistoryManager.LoadHistory();

        // Should return last 1000 items
        Assert.Equal(1000, lines.Count);
        Assert.Equal("command-6", lines.First());
        Assert.Equal("command-1005", lines.Last());

        // File should have been updated and pruned as well
        var fileLines = System.IO.File.ReadAllLines(_tempTestFile);
        Assert.Equal(1000, fileLines.Length);
        Assert.Equal("command-6", fileLines.First());
        Assert.Equal("command-1005", fileLines.Last());
    }

    [Fact]
    public void AppendHistory_ShouldWriteAsyncToHistoryFile()
    {
        ReplHistoryManager.AppendHistory("1 + 2");
        ReplHistoryManager.AppendHistory("3 + 4");

        // Force shutdown to flush queue to file
        ReplHistoryManager.Shutdown();

        Assert.True(System.IO.File.Exists(_tempTestFile));
        var fileLines = System.IO.File.ReadAllLines(_tempTestFile);
        Assert.Equal(2, fileLines.Length);
        Assert.Equal("1 + 2", fileLines[0]);
        Assert.Equal("3 + 4", fileLines[1]);
    }
}
