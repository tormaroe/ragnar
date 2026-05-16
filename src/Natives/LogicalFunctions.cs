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
        }, 1).WithTitle("Returns the logical complement of a value."));

        // and (infix)
        var andNative = new Native((args, refs, _, _, _) =>
        {
            return new Logic(IsTruthy(args[0]) && IsTruthy(args[1]));
        }, 2).WithTitle("Returns true if both values are true.");
        ctx.Set("and", new Op(andNative.Action).WithTitle(andNative.Title));
        ctx.Set("and?", andNative);

        // or (infix)
        var orNative = new Native((args, refs, _, _, _) =>
        {
            return new Logic(IsTruthy(args[0]) || IsTruthy(args[1]));
        }, 2).WithTitle("Returns true if either value is true.");
        ctx.Set("or", new Op(orNative.Action).WithTitle(orNative.Title));
        ctx.Set("or?", orNative);

        // xor (infix)
        var xorNative = new Native((args, refs, _, _, _) =>
        {
            return new Logic(IsTruthy(args[0]) ^ IsTruthy(args[1]));
        }, 2).WithTitle("Returns true if only one of the values is true.");
        ctx.Set("xor", new Op(xorNative.Action).WithTitle(xorNative.Title));
        ctx.Set("xor?", xorNative);
    }
}
