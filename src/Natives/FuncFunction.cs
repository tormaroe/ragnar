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

            var specList = spec.Children.Skip(spec.Index).ToList();
            string title = "";
            if (specList.Count > 0 && specList[0] is Text t)
            {
                title = t.ToUserString();
                specList = specList.Skip(1).ToList();
            }

            // Convert the words in the spec block into a list of strings
            var parameters = specList
                .Select(v => (v as Word)?.Name ?? throw new Exception("Spec must contain words."))
                .ToList();

            return new Function(parameters, body, title);
        }, 2).WithTitle("Defines a function."));

        // return [value]
        ctx.Set("return", new Native((args, refs, _, _, _) => throw new ReturnException(args[0]), 1).WithTitle("Returns a value from a function."));
    }
}
