namespace Ragnar.Natives;

public static class ForeachFunction
{
    public static void Add(Context ctx)
    {
        // foreach line lines [ print line ]
        ctx.Set("foreach", new Native((args, refinements, context, interpreter, isTail) =>
        {
            if (args[0] is Word word && args[1] is Series series && args[2] is Block body)
            {
                Value lastResult = new Word("none");

                try
                {
                    if (series is Block b)
                    {
                        for (int i = b.Index; i < b.Children.Count; i++)
                        {
                            context.Set(word.Name, b.Children[i]);
                            try
                            {
                                lastResult = interpreter.Evaluate(body, context);
                            }
                            catch (ContinueException) { continue; }
                        }
                    }
                    else if (series is Text t)
                    {
                        for (int i = t.Index; i < t.Content.Length; i++)
                        {
                            context.Set(word.Name, new Text(t.Content[i].ToString()));
                            try
                            {
                                lastResult = interpreter.Evaluate(body, context);
                            }
                            catch (ContinueException) { continue; }
                        }
                    }
                }
                catch (BreakException) { }

                return lastResult;
            }
            throw new Exception("foreach usage: foreach word series block");
        }, 3, [false, true, true]).WithTitle("Evaluates a block for each value in a series.")); // Don't evaluate the loop variable name
    }
}
