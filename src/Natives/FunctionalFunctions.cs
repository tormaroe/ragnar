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

        // Partial application
        // partial f x returns a function that calls f x ...
        ctx.Set("partial", new Native((args, refs, context, interpreter, isTail) =>
        {
            return Partial(args[0], args[1]);
        }, 2).WithTitle("Partially applies one argument to a function."));
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

    private static Function Partial(Value f, Value x)
    {
        var closure = new Context(null);
        closure.Set("f", f);
        closure.Set("x", x);

        int originalArity;
        List<(string Name, bool Evaluate)> originalParams = null;
        bool[] evalArgs = null;

        if (f is Native n)
        {
            originalArity = n.Arity;
            evalArgs = n.EvalArgs;
        }
        else if (f is Function func)
        {
            originalArity = func.MainParameters.Count;
            originalParams = func.MainParameters;
        }
        else
        {
            throw new Exception("partial expects a function or native.");
        }

        if (originalArity == 0)
            throw new Exception("Cannot partially apply a zero-argument function.");

        var newParams = new List<(string Name, bool Evaluate)>();
        var body = new Block();
        body.Children.Add(new Word("f", closure));
        body.Children.Add(new Word("x", closure));

        for (int i = 1; i < originalArity; i++)
        {
            string paramName = "p" + i;
            bool evaluate = true;
            if (originalParams != null)
            {
                evaluate = originalParams[i].Evaluate;
                paramName = originalParams[i].Name;
            }
            else if (evalArgs != null)
            {
                evaluate = evalArgs[i];
            }

            newParams.Add((paramName, evaluate));
            body.Children.Add(new GetWord(paramName));
        }

        return new Function(
            newParams,
            new List<(string Name, List<string> Args)>(),
            body,
            "partially-applied"
        );
    }
}
