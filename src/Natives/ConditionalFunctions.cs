namespace Ragnar.Natives;

public static class ConditionalFunctions
{
    public static void Add(Context ctx)
    {
        // if [condition] [block]
        ctx.Set("if", new Native((args, refinements, context, interpreter) =>
        {
            bool isTrue = (args[0] is Logic l && l.Condition);
            if (isTrue && args[1] is Block b)
            {
                return interpreter.Evaluate(b, context);
            }
            return new Word("none");
        }, 2));

        // case [block]
        ctx.Set("case", new Native((args, refinements, context, interpreter) =>
        {
            if (args[0] is not Block b)
                throw new Exception("case requires a block.");

            bool all = refinements.Contains("all");
            Value lastResult = new Word("none");
            bool found = false;

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
                // For now, let's match 'if' logic: Logic(true)
                bool isTrue = (condition is Logic l && l.Condition);

                if (isTrue)
                {
                    // Evaluate the following block
                    Value branch = b.Children[index++];
                    if (branch is Block branchBlock)
                    {
                        lastResult = interpreter.Evaluate(branchBlock, context);
                        found = true;
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
