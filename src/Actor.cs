using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Ragnar.Natives;

namespace Ragnar;

public class ActorInstance
{
    private readonly Channel<Value> _mailbox;
    private readonly CancellationTokenSource _cts = new();

    public ActorInstance()
    {
        _mailbox = Channel.CreateUnbounded<Value>();
    }

    public void Tell(Value message)
    {
        _mailbox.Writer.TryWrite(message);
    }

    public Value Receive()
    {
        try
        {
            // Block until a message is available, respecting cancellation
            return _mailbox.Reader.ReadAsync(_cts.Token).AsTask().GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            throw new Exception("Actor was killed.");
        }
    }

    public void Kill()
    {
        _cts.Cancel();
    }
}

public static class Actor
{
    private static Block? _mezzanineBlock;
    private static readonly object _mezzanineLock = new();

    private static Block GetMezzanineBlock()
    {
        if (_mezzanineBlock == null)
        {
            lock (_mezzanineLock)
            {
                if (_mezzanineBlock == null)
                {
                    var tokens = new Lexer(Mezzanine.SOURCE).Tokenize();
                    _mezzanineBlock = new Loader().Load(tokens);
                }
            }
        }
        return _mezzanineBlock;
    }

    public static void AddActorFunctions(Context ctx)
    {
        ctx.Set("spawn", new Native((args, refinements, context, interpreter, isTail) =>
        {
            if (args.Count != 1 || args[0] is not Block body)
            {
                throw new ArgumentException("spawn expects a single argument of type block.");
            }

            var actor = new ActorInstance();
            
            // Create a fresh context for the actor, isolated from the current one
            // but still having access to the global natives.
            var actorContext = Runtime.CreateGlobalContext();
            actorContext.Output = context.Output;
            
            // Load Mezzanine
            interpreter.Evaluate(GetMezzanineBlock(), actorContext);
            
            actorContext.Set("self", new DotNetValue(actor));

            // Run the actor body in a background task
            Task.Run(() =>
            {
                try
                {
                    interpreter.Evaluate(body, actorContext);
                }
                catch (Exception ex)
                {
                    actorContext.Output.WriteLine($"Actor error: {ex.Message}");
                }
            });

            return new DotNetValue(actor);
        }, 1).WithTitle("Spawn a new actor process."));

        ctx.Set("tell", new Native((args, refinements, context, interpreter, isTail) =>
        {
            if (args.Count != 2 || args[0] is not DotNetValue actorValue || actorValue.Instance is not ActorInstance actor)
            {
                throw new ArgumentException("tell expects an actor and a message.");
            }

            actor.Tell(args[1]);
            return args[1]; // Return message for chaining
        }, 2).WithTitle("Send a message to an actor."));

        ctx.Set("kill", new Native((args, refinements, context, interpreter, isTail) =>
        {
            if (args.Count != 1 || args[0] is not DotNetValue actorValue || actorValue.Instance is not ActorInstance actor)
            {
                throw new ArgumentException("kill expects an actor.");
            }

            actor.Kill();
            return new Word("none");
        }, 1).WithTitle("Terminates an actor process."));

        ctx.Set("receive", new Native((args, refinements, context, interpreter, isTail) =>
        {
            if (context.TryGet("self", out var selfVal) && selfVal is DotNetValue dnv && dnv.Instance is ActorInstance actor)
            {
                return actor.Receive();
            }
            throw new Exception("receive can only be called from within an actor.");
        }, 0).WithTitle("Wait for and return a message from the actor's mailbox."));
    }
}
