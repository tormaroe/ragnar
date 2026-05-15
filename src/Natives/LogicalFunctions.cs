namespace Ragnar.Natives;

public static class LogicalFunctions
{
    private static bool IsTruthy(Value v)
    {
        if (v is Logic l) return l.Condition;
        if (v is Word w && w.Name == "none") return false;
        return true;
    }

    public static void Add(Context ctx)
    {
        // not [condition]
        ctx.Set("not", new Native((args, refs, _, _, _) =>
        {
            return new Logic(!IsTruthy(args[0]));
        }, 1));

        // and [val1] [val2]
        ctx.Set("and", new Native((args, refs, _, _, _) =>
        {
            return new Logic(IsTruthy(args[0]) && IsTruthy(args[1]));
        }, 2));
        ctx.Set("and?", new Word("and"));

        // or [val1] [val2]
        ctx.Set("or", new Native((args, refs, _, _, _) =>
        {
            return new Logic(IsTruthy(args[0]) || IsTruthy(args[1]));
        }, 2));
        ctx.Set("or?", new Word("or"));
    }
}
