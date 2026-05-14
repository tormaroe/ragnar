using Xunit;
using Ragnar;
using System;

namespace Ragnar.Tests;

public class ControlFlowTests : TestBase
{
    [Fact]
    public void Break_Works()
    {
        var code = @"
            a: 0
            while [true] [
                a: a + 1
                if (a = 5) [break]
            ]
            a
        ";
        var (result, _) = Run(code);
        Assert.Equal(5, ((Integer)result).Number);
    }

    [Fact]
    public void Continue_Works()
    {
        var code = @"
            a: 0
            b: 0
            while [a < 10] [
                a: a + 1
                if (a // 2 = 0) [continue]
                b: b + 1
            ]
            b
        ";
        var (result, _) = Run(code);
        Assert.Equal(5, ((Integer)result).Number);
    }

    [Fact]
    public void Return_Works()
    {
        var code = @"
            f: func [x] [
                if (x < 0) [return ""negative""]
                ""positive""
            ]
            r1: f -1
            r2: f 1
        ";
        var (_, ctx) = Run(code);
        Assert.Equal("negative", ((Text)ctx.Get("r1")).ToUserString());
        Assert.Equal("positive", ((Text)ctx.Get("r2")).ToUserString());
    }
}
