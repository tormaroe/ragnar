using Xunit;
using Ragnar;
using System;
using System.IO;
using System.Text;

namespace Ragnar.Tests;

public class ReadmeTests : TestBase
{
    [Fact]
    public void Test_ConfigurationSection()
    {
        // Demonstrating that we can join home and rc-file-name
        var code = @"
            config-path: join home rc-file-name
        ";
        var (result, _) = Run(code);
        Assert.IsType<Ragnar.File>(result);
    }

    [Fact]
    public void Test_ReplAndReflection()
    {
        // type? queries
        var (resInteger, _) = Run("type? 42");
        Assert.Equal("integer!", resInteger.ToString());

        var (resText, _) = Run("type? \"hello\"");
        Assert.Equal("text!", resText.ToString());

        var (resBlock, _) = Run("type? [1 2 3]");
        Assert.Equal("block!", resBlock.ToString());

        // help output
        var (_, helpOutput) = RunWithOutput("help add");
        Assert.Contains("WORD: add", helpOutput);
        Assert.Contains("TYPE:  Native Function", helpOutput);

        // what output
        var (_, whatOutput) = RunWithOutput("what");
        Assert.Contains("add", whatOutput);
        Assert.Contains("print", whatOutput);

        // probe output and return value
        var (probeVal, probeOutput) = RunWithOutput("probe 10 + 20");
        Assert.Equal("30", probeOutput);
        Assert.Equal(30, ((Integer)probeVal).Number);
    }

    [Fact]
    public void Test_CoreRagnarFeatures()
    {
        var code = @"
            ; Variable assignment
            name: ""Ragnar""
            age: 25
            
            ; Conditionals
            status: either age > 18 [ ""adult"" ] [ ""minor"" ]
            
            ; Foreach and filtering
            data: [10 21 30 43 50]
            evens: []
            foreach n data [
                if (n // 2) == 0 [ append evens n ]
            ]
            
            ; Series manipulation
            first-even: pick evens 1
            second-even: evens/2
            
            reduce [status evens first-even second-even]
        ";
        var (result, _) = Run(code);
        var b = Assert.IsType<Block>(result);
        
        Assert.Equal("\"adult\"", b.Children[0].ToString());
        Assert.Equal("[ 10 30 50 ]", b.Children[1].ToString());
        Assert.Equal(10, ((Integer)b.Children[2]).Number);
        Assert.Equal(30, ((Integer)b.Children[3]).Number);
    }

    [Fact]
    public void Test_DotNetInterop()
    {
        var code = @"
            ; Instantiating with new
            builder: new ""System.Text.StringBuilder"" [""Hello""]
            
            ; Calling instance methods
            call-method builder ""Append"" ["" World""]
            
            ; Path navigation (instance property)
            len: builder/Length
            
            ; Path navigation (static property)
            pi: System.Math/PI
            
            ; Path navigation (modifying property)
            builder/Length: 5
            short-str: call-method builder ""ToString"" []
            
            reduce [len pi short-str]
        ";
        var (result, _) = Run(code);
        var b = Assert.IsType<Block>(result);

        Assert.Equal(11, ((Integer)b.Children[0]).Number);
        Assert.Equal(Math.PI, ((Decimal)b.Children[1]).Number);
        Assert.Equal("Hello", ((Text)b.Children[2]).Content);
    }

    [Fact]
    public void Test_ObjectSupport()
    {
        var code = @"
            square: make object! [
                side: 0
                area: does [ self/side * self/side ]
                perimeter: does [ 4 * self/side ]
                multiply: func [x] [
                    self/side: x * self/side
                ]
            ]

            square/side: 3
            square/multiply 2
            a1: square/area
            p1: square/perimeter
            
            ; bind and in
            word: in square 'side
            val: get word
            
            ; get-path support
            fn: :square/multiply
            fn-type: type? :fn
            
            area-fn: :square/area
            area-fn-type: type? :area-fn
            
            reduce [a1 p1 val fn-type area-fn-type]
        ";
        var (result, _) = Run(code);
        var b = Assert.IsType<Block>(result);

        Assert.Equal(36, ((Integer)b.Children[0]).Number);
        Assert.Equal(24, ((Integer)b.Children[1]).Number);
        Assert.Equal(6, ((Integer)b.Children[2]).Number);
        Assert.Equal("function!", b.Children[3].ToString());
        Assert.Equal("function!", b.Children[4].ToString());
    }

    [Fact]
    public void Test_Parse()
    {
        // 1. Simple delimiter splitting
        var splitCode = "parse \"alice,30,engineer\" \",\"";
        var (splitRes, _) = Run(splitCode);
        var splitBlock = Assert.IsType<Block>(splitRes);
        Assert.Equal(3, splitBlock.Children.Count);
        Assert.Equal("alice", ((Text)splitBlock.Children[0]).Content);
        Assert.Equal("30", ((Text)splitBlock.Children[1]).Content);
        Assert.Equal("engineer", ((Text)splitBlock.Children[2]).Content);

        // 2. Dialect pattern matching
        var dialectCode = @"
            digits: charset ""0123456789""
            phone-num: [3 digits ""-"" 4 digits]
            parse ""467-8000"" phone-num
        ";
        var (dialectRes, _) = Run(dialectCode);
        Assert.True(((Logic)dialectRes).Condition);
    }

    [Fact]
    public void Test_TailCallOptimization()
    {
        // Factorial test from existing TOC documentation
        var code = @"
            factorial: func [n] [
                loop: func [i accum] [
                    either i > n [
                        accum
                    ] [
                        loop (i + 1) (accum * i)
                    ]
                ]
                loop 1 1 
            ]
            factorial 10
        ";
        var (result, _) = Run(code);
        Assert.Equal(3628800, ((Integer)result).Number);
    }

    [Fact]
    public void Test_ActorModel()
    {
        var code = @"
            start-area-server: does [
                spawn [
                    forever [
                        msg: receive
                        client: first msg
                        shape: second msg
                        switch/default first shape [
                            rectangle [
                                tell client reform [
                                    ""area of rectangle is"" (shape/2 * shape/3) ]
                            ]
                            circle [
                                tell client reform [ 
                                    ""area of circle is"" (3.14159 * (shape/2 * shape/2)) ]
                            ]
                        ] [
                            tell client reform [ 
                                ""i don't know what the area of a"" shape/1 ""is."" ] 
                        ]
                    ]
                ]
            ]

            server: start-area-server
            tell server [rectangle 5 10]
            res1: second receive
            tell server [circle 5]
            res2: second receive
            tell server [triangle 5 10]
            res3: second receive
            kill server
            
            ; wait for server to die
            limit: 500
            while [all [limit > 0 find system/actors server]] [
                wait 10
                limit: limit - 1
            ]
            
            reduce [res1 res2 res3]
        ";
        var (result, _) = Run(code);
        var b = Assert.IsType<Block>(result);
        Assert.Equal("\"area of rectangle is 50\"", b.Children[0].ToString());
        Assert.Equal("\"area of circle is 78.53975\"", b.Children[1].ToString());
        Assert.Equal("\"i don't know what the area of a triangle is.\"", b.Children[2].ToString());
    }

    [Fact]
    public void Test_FunctionalProgramming()
    {
        var code = @"
            ; Closures capturing scope
            make-counter: func [start] [
                func [] [
                    current: start
                    start: start + 1
                    current
                ]
            ]
            counter: make-counter 10
            c1: counter
            c2: counter
            
            ; Partial application
            add-five: partial :add 5
            p1: add-five 10
            
            ; Composition pipeline
            inc: func [n] [n + 1]
            double: func [n] [n * 2]
            
            f-forward: :inc >> :double ; (x + 1) * 2
            f-backward: :inc << :double ; (x * 2) + 1
            
            res-f: f-forward 5
            res-b: f-backward 5
            
            reduce [c1 c2 p1 res-f res-b]
        ";
        var (result, _) = Run(code);
        var b = Assert.IsType<Block>(result);
        
        Assert.Equal(10, ((Integer)b.Children[0]).Number);
        Assert.Equal(11, ((Integer)b.Children[1]).Number);
        Assert.Equal(15, ((Integer)b.Children[2]).Number);
        Assert.Equal(12, ((Integer)b.Children[3]).Number);
        Assert.Equal(11, ((Integer)b.Children[4]).Number);
    }

    [Fact]
    public void Test_FunctionalMezzanines()
    {
        var code = @"
            ; 1. funcmap - Applies a function to each item
            double: func [x] [x * 2]
            r1: funcmap :double [1 2 3]

            ; 2. funcflatmap - Applies a function and flattens the results
            expand: func [x] [reduce [x x * 10]]
            r2: funcflatmap :expand [1 2 3]

            ; 3. funcfilter - Keeps items where the function returns true
            even?: func [x] [x // 2 = 0]
            r3: funcfilter :even? [1 2 3 4 5 6]

            ; 4. funcfold - Reduces a block using a binary function (optional /initial)
            sum: func [a b] [a + b]
            r4: funcfold :sum [1 2 3 4]
            r5: funcfold/initial :sum [1 2 3 4] 10

            reduce [r1 r2 r3 r4 r5]
        ";
        var (result, _) = Run(code);
        Assert.Equal("[ [ 2 4 6 ] [ 1 10 2 20 3 30 ] [ 2 4 6 ] 10 20 ]", result.ToString());
    }
}

