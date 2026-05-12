namespace Ragnar.Natives;

public static class WhileFunction
{
    public static void Add(Context ctx)
    {
        // while [ condition-block ] [ body-block ]
        ctx.Set("while", new Native((args, refinements, context, interpreter) =>
        {
            if (args[0] is Block condition && args[1] is Block body)
            {
                Value lastResult = new Word("none");

                // Keep evaluating the condition block. 
                // If it returns Logic(true), run the body.
                while (true)
                {
                    Value condResult = interpreter.Evaluate(condition, context);
                    if (condResult is Logic l && l.Condition)
                    {
                        lastResult = interpreter.Evaluate(body, context);
                    }
                    else
                    {
                        break;
                    }
                }
                return lastResult;
            }
            throw new Exception("while usage: while [condition-block] [body-block]");
        }, 2));
    }
}
