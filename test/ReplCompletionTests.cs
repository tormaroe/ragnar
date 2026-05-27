using Xunit;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Ragnar;

namespace Ragnar.Tests;

public class ReplCompletionTests
{
    private Context CreateTestContext()
    {
        // Use a clean, isolated context with no built-ins to ensure deterministic completion matching
        var context = new Context();
        context.Set("either", new Word("none"));
        context.Set("enumerate", new Word("none"));
        context.Set("even?", new Word("none"));
        context.Set("seed", new Word("none"));
        return context;
    }

    [Fact(Timeout = 2000)]
    public async Task SingleMatchCompletion()
    {
        await Task.Yield();
        var repl = new Repl();
        var context = CreateTestContext();

        var keys = new Queue<ConsoleKeyInfo>(new[]
        {
            new ConsoleKeyInfo('e', ConsoleKey.E, false, false, false),
            new ConsoleKeyInfo('i', ConsoleKey.I, false, false, false),
            new ConsoleKeyInfo('t', ConsoleKey.T, false, false, false),
            new ConsoleKeyInfo('h', ConsoleKey.H, false, false, false),
            new ConsoleKeyInfo('\0', ConsoleKey.Tab, false, false, false),
            new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false)
        });
        repl.ReadKeyFunc = () => keys.Dequeue();

        var result = repl.ReadLine(">> ", context);
        Assert.Equal("either", result);
    }

    [Fact(Timeout = 2000)]
    public async Task MultipleMatchesCyclingForward()
    {
        await Task.Yield();
        var repl = new Repl();
        var context = CreateTestContext();

        var keys = new Queue<ConsoleKeyInfo>(new[]
        {
            new ConsoleKeyInfo('e', ConsoleKey.E, false, false, false),
            new ConsoleKeyInfo('\0', ConsoleKey.Tab, false, false, false), // completes to either (matches: either, enumerate, even?)
            new ConsoleKeyInfo('\0', ConsoleKey.Tab, false, false, false), // cycles to enumerate
            new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false)
        });
        repl.ReadKeyFunc = () => keys.Dequeue();

        var result = repl.ReadLine(">> ", context);
        Assert.Equal("enumerate", result);
    }

    [Fact(Timeout = 2000)]
    public async Task MultipleMatchesCyclingBackward()
    {
        await Task.Yield();
        var repl = new Repl();
        var context = CreateTestContext();

        var keys = new Queue<ConsoleKeyInfo>(new[]
        {
            new ConsoleKeyInfo('e', ConsoleKey.E, false, false, false),
            new ConsoleKeyInfo('\0', ConsoleKey.Tab, false, false, false),       // completes to either
            new ConsoleKeyInfo('\0', ConsoleKey.LeftArrow, false, false, false),  // cycles backward to even?
            new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false)
        });
        repl.ReadKeyFunc = () => keys.Dequeue();

        var result = repl.ReadLine(">> ", context);
        Assert.Equal("even?", result);
    }

    [Fact(Timeout = 2000)]
    public async Task EscapeCancelsCompletion()
    {
        await Task.Yield();
        var repl = new Repl();
        var context = CreateTestContext();

        // Must use a prefix with multiple matches ('e') to trigger cycling mode, where Escape is handled
        var keys = new Queue<ConsoleKeyInfo>(new[]
        {
            new ConsoleKeyInfo('e', ConsoleKey.E, false, false, false),
            new ConsoleKeyInfo('\0', ConsoleKey.Tab, false, false, false),    // cycles to either
            new ConsoleKeyInfo('\0', ConsoleKey.Escape, false, false, false), // cancels and restores 'e'
            new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false)
        });
        repl.ReadKeyFunc = () => keys.Dequeue();

        var result = repl.ReadLine(">> ", context);
        Assert.Equal("e", result);
    }

    [Fact(Timeout = 2000)]
    public async Task DelimitersRespected()
    {
        await Task.Yield();
        var repl = new Repl();
        var context = CreateTestContext();

        // Testing bracket [, curly brace {, and slash / as delimiters
        var testCases = new[] { '[', '{', '/' };

        foreach (var delimiter in testCases)
        {
            var keys = new Queue<ConsoleKeyInfo>(new[]
            {
                new ConsoleKeyInfo(delimiter, ConsoleKey.NoName, false, false, false),
                new ConsoleKeyInfo('e', ConsoleKey.E, false, false, false),
                new ConsoleKeyInfo('i', ConsoleKey.I, false, false, false),
                new ConsoleKeyInfo('t', ConsoleKey.T, false, false, false),
                new ConsoleKeyInfo('h', ConsoleKey.H, false, false, false),
                new ConsoleKeyInfo('\0', ConsoleKey.Tab, false, false, false),
                new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false)
            });
            repl.ReadKeyFunc = () => keys.Dequeue();

            var result = repl.ReadLine(">> ", context);
            Assert.Equal($"{delimiter}either", result);
        }
    }
}
