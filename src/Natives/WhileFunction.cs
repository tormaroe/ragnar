namespace Ragnar.Natives;

public static class WhileFunction
{
    public static void Add(Context ctx)
    {
        // while [ condition-block ] [ body-block ]
        ctx.Set("while", new Native((args, refinements, context, interpreter, isTail) =>
        {
            if (args[0] is Block condition && args[1] is Block body)
            {
                Value lastResult = new Word("none");

                try
                {
                    while (true)
                    {
                        Value condResult = interpreter.Evaluate(condition, context, false);
                        
                        // Rebol-style truthiness: everything except false and none is true
                        bool isTrue = true;
                        if (condResult is Logic l && !l.Condition) isTrue = false;
                        else if (condResult is Word w && w.Name == "none") isTrue = false;

                        if (isTrue)
                        {
                            try
                            {
                                lastResult = interpreter.Evaluate(body, context, false);
                            }
                            catch (ContinueException) { /* just continue loop */ }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                catch (BreakException) { /* break out of loop */ }

                return lastResult;
            }
            throw new Exception("while usage: while [condition-block] [body-block]");
        }, 2).WithTitle("Repeatedly executes a block while a condition is true."));
    }
}
