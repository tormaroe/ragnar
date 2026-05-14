namespace Ragnar.Natives;

public static class ConditionalFunctions
{
    public static void Add(Context ctx)
    {
        static bool IsTruthy(Value v)
        {
            if (v is Logic l) return l.Condition;
            if (v is Word w && w.Name == "none") return false;
            return true;
        }

        // if [condition] [block]
        ctx.Set("if", new Native((args, refinements, context, interpreter) =>
        {
            if (IsTruthy(args[0]) && args[1] is Block b)
            {
                return interpreter.Evaluate(b, context);
            }
            return new Word("none");
        }, 2));

        // either [condition] [true-block] [false-block]
        ctx.Set("either", new Native((args, refinements, context, interpreter) =>
        {
            Block branch = (IsTruthy(args[0]) ? args[1] : args[2]) as Block 
                ?? throw new Exception("either requires blocks for its branches.");
            
            return interpreter.Evaluate(branch, context);
        }, 3));

        // all [block]
        ctx.Set("all", new Native((args, refinements, context, interpreter) =>
        {
            if (args[0] is not Block b) throw new Exception("all requires a block.");
            Value lastResult = new Word("none");
            int index = 0;
            while (index < b.Children.Count)
            {
                lastResult = interpreter.Next(b, ref index, context);
                if (!IsTruthy(lastResult)) return new Word("none");
            }
            return lastResult;
        }, 1));

        // any [block]
        ctx.Set("any", new Native((args, refinements, context, interpreter) =>
        {
            if (args[0] is not Block b) throw new Exception("any requires a block.");
            int index = 0;
            while (index < b.Children.Count)
            {
                Value result = interpreter.Next(b, ref index, context);
                if (IsTruthy(result)) return result;
            }
            return new Word("none");
        }, 1));

        // case [block]
        ctx.Set("case", new Native((args, refinements, context, interpreter) =>
        {
            if (args[0] is not Block b)
                throw new Exception("case requires a block.");

            bool all = refinements.Contains("all");
            Value lastResult = new Word("none");

            int index = 0;
            while (index < b.Children.Count)
            {
                // Evaluate condition
                Value condition = interpreter.Next(b, ref index, context);
                
                // If there's no block following the condition, it's an error in Rebol, 
                // but let's be safe and check.
                if (index >= b.Children.Count) break;

                // Condition is true if it's a Logic true, or in Rebol/Ragnar 
                // usually any value except false/none is true. 
                bool isTrue = IsTruthy(condition);

                if (isTrue)
                {
                    // Evaluate the following block
                    Value branch = b.Children[index++];
                    if (branch is Block branchBlock)
                    {
                        lastResult = interpreter.Evaluate(branchBlock, context);
                        if (!all) return lastResult;
                    }
                }
                else
                {
                    // Skip the following block
                    index++;
                }
            }

            return lastResult;
        }, 1));
    }
}
