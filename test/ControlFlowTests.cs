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

    [Fact]
    public void Deep_Recursion_Succeeds_With_TCO()
    {
        // This depth (2,000) is enough to verify TCO works (default stack would likely blow)
        // while avoiding excessive slowness due to O(depth^2) context lookup.
        var code = @"
            count: func [n acc] [
                either n = 0 [
                    acc
                ] [
                    count (n - 1) (acc + 1)
                ]
            ]
            count 2000 0
        ";
        var (result, _) = Run(code);
        Assert.Equal(2000, ((Integer)result).Number);
    }

    [Fact]
    public void Euler_1_Solution_2_Works()
    {
        var code = @"
            include?: func [x] [ or (x // 3 = 0) (x // 5 = 0) ]

            f: func [limit] [
                 inner: func [acc n] [
                      either n > 0 [
                        if include? n [
                          acc: acc + n
                        ]
                        inner acc (n - 1)
                      ] [
                        acc
                      ]
                 ]
                 inner 0 (limit - 1)
            ]
            f 1000
        ";
        var (result, _) = Run(code);
        // Solution to Euler 1 for 1000 is 233168
        Assert.Equal(233168, ((Integer)result).Number);
    }

    [Fact]
    public void Forever_Works()
    {
        var code = @"
            a: 0
            forever [
                a: a + 1
                if (a = 10) [break]
            ]
            a
        ";
        var (result, _) = Run(code);
        Assert.Equal(10, ((Integer)result).Number);
    }
}
