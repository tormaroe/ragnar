namespace Ragnar.Natives;

public static class IfFunction
{
    public static void Add(Context ctx)
    {
        // 4. if [condition] [block]
        ctx.Set("if", new Native((args, refinements, context, interpreter) =>
        {
            bool isTrue = (args[0] is Logic l && l.Condition);
            if (isTrue && args[1] is Block b)
            {
                return interpreter.Evaluate(b, context);
            }
            return new Word("none");
        }, 2));
    }
}
