namespace Ragnar.Natives;

public static class LoopFunction
{
    public static void Add(Context ctx)
    {
        // loop 5 [ print "Hello" ]
        ctx.Set("loop", new Native((args, refinements, context, interpreter) =>
        {
            if (args[0] is Integer count && args[1] is Block body)
            {
                Value lastResult = new Word("none");
                for (long i = 0; i < count.Number; i++)
                {
                    lastResult = interpreter.Evaluate(body, context);
                }
                return lastResult;
            }
            throw new Exception("loop usage: loop [integer] [block]");
        }, 2));
    }
}
