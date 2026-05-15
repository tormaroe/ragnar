namespace Ragnar.Natives;

public static class FuncFunction
{
    public static void Add(Context ctx)
    {
        // func [spec] [body]
        ctx.Set("func", new Native((args, refinements, context, interpreter, isTail) =>
        {
            if (args[0] is not Block spec) throw new Exception("func spec must be a block.");
            if (args[1] is not Block body) throw new Exception("func body must be a block.");

            // Convert the words in the spec block into a list of strings
            var parameters = spec.Children
                .Select(v => (v as Word)?.Name ?? throw new Exception("Spec must contain words."))
                .ToList();

            return new Function(parameters, body);
        }, 2));

        // return [value]
        ctx.Set("return", new Native((args, refs, _, _, _) => throw new ReturnException(args[0]), 1));
    }
}
