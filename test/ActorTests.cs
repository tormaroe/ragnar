using Xunit;
using Ragnar;
using System.Threading.Tasks;

namespace Ragnar.Tests;

public class ActorTests : TestBase
{
    [Fact(Timeout = 5000)]
    public async Task TestSpawnAndReceive()
    {
        var script = @"
            a: spawn [
                req: receive
                client: first req
                msg: second req
                tell client msg
            ]
            tell a ""hello-from-actor""
            res: receive
            second res
        ";
        
        var (result, ctx) = Run(script);
        Assert.Equal("\"hello-from-actor\"", result.ToString());
        await Task.CompletedTask;
    }

    [Fact(Timeout = 5000)]
    public async Task TestActorLoop()
    {
        var script = @"
            a: spawn [
                while [true] [
                    req: receive
                    client: first req
                    msg: second req
                    if msg = ""quit"" [
                        tell client ""done""
                        break
                    ]
                    tell client join ""Got:"" msg
                ]
            ]
            tell a ""hello""
            res1: second receive
            tell a ""world""
            res2: second receive
            tell a ""quit""
            res3: second receive
            reduce [res1 res2 res3]
        ";
        
        var (result, ctx) = Run(script);
        var resBlock = (Block)result;
        Assert.Equal("\"Got:hello\"", resBlock.Children[0].ToString());
        Assert.Equal("\"Got:world\"", resBlock.Children[1].ToString());
        Assert.Equal("\"done\"", resBlock.Children[2].ToString());
        await Task.CompletedTask;
    }

    [Fact(Timeout = 5000)]
    public async Task TestKillActor()
    {
        var script = @"
            a: spawn [
                req: receive
                client: first req
                tell client ""started""
                receive
                print ""Actor received msg""
            ]
            tell a ""start""
            receive ; wait for started
            kill a
            
            ; Wait until a is removed from system/actors
            limit: 500
            while [all [limit > 0 find system/actors a]] [
                wait 10
                limit: limit - 1
            ]
            
            tell a ""msg""
        ";
        
        var (result, output) = RunWithOutput(script);
        Assert.Contains("Actor error: Actor was killed.", output);
        Assert.DoesNotContain("Actor received msg", output);
        await Task.CompletedTask;
    }

    [Fact(Timeout = 5000)]
    public async Task TestSystemActors()
    {
        var script = @"
            initial: length? system/actors
            a: spawn [ receive ]
            b: spawn [ receive ]
            count1: (length? system/actors) - initial
            kill a
            
            ; Wait until a is removed from system/actors
            limit: 500
            while [all [limit > 0 find system/actors a]] [
                wait 10
                limit: limit - 1
            ]
            
            count2: (length? system/actors) - initial
            kill b ; cleanup
            reduce [count1 count2]
        ";
        
        var (result, ctx) = Run(script);
        var resBlock = (Block)result;
        Assert.Equal(2, ((Integer)resBlock.Children[0]).Number);
        Assert.Equal(1, ((Integer)resBlock.Children[1]).Number);
        await Task.CompletedTask;
    }

    [Fact(Timeout = 5000)]
    public async Task TestRpc()
    {
        var script = @"
            server: spawn [
                while [true] [
                    req: receive
                    client: first req
                    msg: second req
                    tell client join ""echo:"" msg
                ]
            ]
            tell server ""hello""
            res: second receive
            kill server
            
            ; Wait for server to die
            limit: 500
            while [all [limit > 0 find system/actors server]] [
                wait 10
                limit: limit - 1
            ]
            
            res
        ";
        
        var (result, ctx) = Run(script);
        Assert.Equal("\"echo:hello\"", result.ToString());
        await Task.CompletedTask;
    }
}
