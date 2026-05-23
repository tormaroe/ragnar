using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Ragnar.Natives;

namespace Ragnar;

public class ActorInstance
{
    private readonly Channel<Value> _mailbox;
    private readonly CancellationTokenSource _cts = new();
    private volatile bool _killed = false;

    public ActorInstance()
    {
        _mailbox = Channel.CreateUnbounded<Value>();
    }

    public void Tell(Value message)
    {
        // Silently drop messages sent to a killed actor.
        if (!_killed)
            _mailbox.Writer.TryWrite(message);
    }

    public Value Receive()
    {
        try
        {
            // Block until a message is available, respecting cancellation.
            var value = _mailbox.Reader.ReadAsync(_cts.Token).AsTask().GetAwaiter().GetResult();
            // Even if a message arrived just as Kill() fired, honour the kill.
            if (_killed)
                throw new OperationCanceledException(_cts.Token);
            return value;
        }
        catch (OperationCanceledException)
        {
            throw new Exception("Actor was killed.");
        }
    }

    public void Kill()
    {
        _killed = true;
        _cts.Cancel();
    }
}

public static class Actor
{
    private static readonly ConcurrentDictionary<ActorInstance, byte> _activeActors = new();
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

    private static void SyncActors()
    {
        var systemVal = Runtime.SystemObject;
        if (systemVal != null && systemVal.Context.TryGet("actors", out var actorsVal) && actorsVal is Block actorsBlock)
        {
            lock (actorsBlock)
            {
                actorsBlock.Children.Clear();
                foreach (var actor in _activeActors.Keys)
                {
                    actorsBlock.Children.Add(new DotNetValue(actor));
                }
            }
        }
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
            _activeActors.TryAdd(actor, 0);
            SyncActors();
            
            // Create a fresh context for the actor, isolated from the current one
            // but still having access to the global natives.
            var actorContext = Runtime.CreateGlobalContext();
            actorContext.Output = context.Output;
            
            // Load Mezzanine
            interpreter.Evaluate(GetMezzanineBlock(), actorContext);
            
            actorContext.SetLocal("self", new DotNetValue(actor));

            // Run the actor body in a background task
            _ = Task.Run(() =>
            {
                try
                {
                    interpreter.Evaluate(body, actorContext);
                }
                catch (Exception ex)
                {
                    actorContext.Output.WriteLine($"Actor error: {ex.Message}");
                }
                finally
                {
                    _activeActors.TryRemove(actor, out _);
                    SyncActors();
                }
            });

            return new DotNetValue(actor);
        }, 1).WithTitle("Spawn a new actor process."));

        ctx.Set("tell", new Native((args, refinements, context, interpreter, isTail) =>
        {
            if (args.Count != 2 || args[0] is not DotNetValue actorValue || actorValue.Instance is not ActorInstance recipient)
            {
                throw new ArgumentException("tell expects an actor and a message.");
            }

            var message = args[1];

            // Look up "self" in the context to determine the sender
            Value senderVal = new Word("none");
            if (context.TryGet("self", out var selfVal) && selfVal is DotNetValue dnv && dnv.Instance is ActorInstance)
            {
                senderVal = selfVal;
            }

            // Construct envelope block: [sender message]
            var envelope = new Block();
            envelope.Children.Add(senderVal);
            envelope.Children.Add(message);

            recipient.Tell(envelope);
            return message;
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
