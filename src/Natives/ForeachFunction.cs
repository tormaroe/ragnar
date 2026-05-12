namespace Ragnar.Natives;

public static class ForeachFunction
{
    public static void Add(Context ctx)
    {
        // foreach line lines [ print line ]
        ctx.Set("foreach", new Native((args, refinements, context, interpreter) =>
        {
            if (args[0] is Word word && args[1] is Block series && args[2] is Block body)
            {
                Value lastResult = new Word("none");

                foreach (var item in series.Children)
                {
                    // 1. Set the loop variable in the current context
                    context.Set(word.Name, item);

                    // 2. Evaluate the body
                    lastResult = interpreter.Evaluate(body, context);
                }

                return lastResult;
            }
            throw new Exception("foreach usage: foreach word series block");
        }, 3, [false, true, true])); // Don't evaluate the loop variable name
    }
}
