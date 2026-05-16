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

            var mainParams = new List<string>();
            var userRefinements = new List<(string Name, List<string> Args)>();

            int i = 0;
            while (i < specList.Count && specList[i] is Word w)
            {
                mainParams.Add(w.Name);
                i++;
            }

            while (i < specList.Count)
            {
                if (specList[i] is Refinement r)
                {
                    var refArgs = new List<string>();
                    i++;
                    while (i < specList.Count && specList[i] is Word argW)
                    {
                        refArgs.Add(argW.Name);
                        i++;
                    }
                    userRefinements.Add((r.Name, refArgs));
                }
                else
                {
                    throw new Exception($"Unexpected token {specList[i]} in func spec. Parameters must come before refinements.");
                }
            }

            return new Function(mainParams, userRefinements, body, title);
        }, 2).WithTitle("Defines a function."));

        // return [value]
        ctx.Set("return", new Native((args, refs, _, _, _) => throw new ReturnException(args[0]), 1).WithTitle("Returns a value from a function."));
    }
}
