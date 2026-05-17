using System;
using System.Collections.Generic;
using System.Linq;

namespace Ragnar.Natives;

public static class FunctionalFunctions
{
    public static void Add(Context ctx)
    {
        // Forward composition (>>)
        // (f >> g) x = g(f(x))
        ctx.Set(">>", new Op((args, refs, context, interpreter, isTail) =>
        {
            return Compose(args[0], args[1], true);
        }).WithTitle("Forward function composition: (f >> g) x => g(f(x))"));

        // Backward composition (<<)
        // (f << g) x = f(g(x))
        ctx.Set("<<", new Op((args, refs, context, interpreter, isTail) =>
        {
            return Compose(args[0], args[1], false);
        }).WithTitle("Backward function composition: (f << g) x => f(g(x))"));
    }

    private static Function Compose(Value f, Value g, bool forward)
    {
        // We use a closure context to bind the functions.
        var closure = new Context(null);
        closure.Set("f", f);
        closure.Set("g", g);

        var body = new Block();
        if (forward)
        {
            // g f :x
            body.Children.Add(new Word("g", closure));
            body.Children.Add(new Word("f", closure));
            body.Children.Add(new GetWord("x"));
        }
        else
        {
            // f g :x
            body.Children.Add(new Word("f", closure));
            body.Children.Add(new Word("g", closure));
            body.Children.Add(new GetWord("x"));
        }

        return new Function(
            new List<(string Name, bool Evaluate)> { ("x", true) },
            new List<(string Name, List<string> Args)>(),
            body,
            forward ? "forward-composed" : "backward-composed"
        );
    }
}
