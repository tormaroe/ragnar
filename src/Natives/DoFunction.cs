namespace Ragnar.Natives;

public static class DoFunction
{
    public static void Add(Context ctx)
    {
        // 3. do [block]
        // This is the core of homoiconicity: treating data as code.
        ctx.Set("do", new Native((args, refinements, context, interpreter, isTail) =>
        {
            if (args[0] is Block b) return interpreter.Evaluate(b, context, isTail);
            return args[0]; // If not a block, just return the value
        }, 1).WithTitle("Evaluates a block of code or returns a value."));
    }
}
