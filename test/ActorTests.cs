using Xunit;
using Ragnar;

namespace Ragnar.Tests;

public class ActorTests : TestBase
{
    [Fact]
    public void TestSpawnAndReceive()
    {
        var script = @"
            a: spawn [
                msg: receive
                print msg
            ]
            tell a ""hello-from-actor""
            wait 200
        ";
        
        var (result, output) = RunWithOutput(script);
        Assert.Contains("hello-from-actor", output);
    }

    [Fact]
    public void TestActorLoop()
    {
        var script = @"
            a: spawn [
                while [true] [
                    msg: receive
                    if msg = ""quit"" [ break ]
                    print [""Got:"" msg]
                ]
            ]
            tell a ""hello""
            tell a ""world""
            tell a ""quit""
            wait 300
        ";
        
        var (result, output) = RunWithOutput(script);
        Assert.Contains("Got: hello", output);
        Assert.Contains("Got: world", output);
        Assert.DoesNotContain("Got: quit", output);
    }

    [Fact]
    public void TestKillActor()
    {
        var script = @"
            a: spawn [
                print ""Actor starting""
                receive
                print ""Actor received msg""
            ]
            wait 100
            kill a
            tell a ""msg""
            wait 100
        ";
        
        var (result, output) = RunWithOutput(script);
        Assert.Contains("Actor starting", output);
        Assert.Contains("Actor error: Actor was killed.", output);
        Assert.DoesNotContain("Actor received msg", output);
    }

    [Fact]
    public void TestSystemActors()
    {
        var script = @"
            a: spawn [ receive ]
            b: spawn [ receive ]
            wait 100
            count1: length? system/actors
            kill a
            wait 100
            count2: length? system/actors
            kill b ; cleanup
            reduce [count1 count2]
        ";
        
        var (result, ctx) = Run(script);
        var resBlock = (Block)result;
        Assert.Equal(2, ((Integer)resBlock.Children[0]).Number);
        Assert.Equal(1, ((Integer)resBlock.Children[1]).Number);
    }
}
