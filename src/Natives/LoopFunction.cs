namespace Ragnar.Natives;

public static class LoopFunction
{
    public static void Add(Context ctx)
    {
        // loop 5 [ print "Hello" ]
        ctx.Set("loop", new Native((args, refinements, context, interpreter, isTail) =>
        {
            if (args[0] is Integer count && args[1] is Block body)
            {
                Value lastResult = new Word("none");
                try
                {
                    for (long i = 0; i < count.Number; i++)
                    {
                        try
                        {
                            lastResult = interpreter.Evaluate(body, context);
                        }
                        catch (ContinueException) { continue; }
                    }
                }
                catch (BreakException) { }
                return lastResult;
            }
            throw new Exception("loop usage: loop [integer] [block]");
        }, 2).WithTitle("Evaluates a block a specified number of times."));

        // break
        ctx.Set("break", new Native((args, refs, _, _, _) => throw new BreakException(), 0).WithTitle("Breaks out of a loop."));

        // continue
        ctx.Set("continue", new Native((args, refs, _, _, _) => throw new ContinueException(), 0).WithTitle("Skips to the next iteration of a loop."));
    }
}
