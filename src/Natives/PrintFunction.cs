namespace Ragnar.Natives;

public static class PrintFunction
{
    public static void Add(Context ctx)
    {
        ctx.Set("print", new Native((args, refs, context, interpreter, isTail) =>
        {
            var val = args[0];

            if (val is Block b)
            {
                // 1. Reduce the block (evaluate all the 'add', 'now/year', etc.)
                var reduced = new List<Value>();
                int index = 0;
                while (index < b.Children.Count)
                {
                    reduced.Add(interpreter.Next(b, ref index, context, false));
                }

                // 2. Join the results with spaces and print
                var output = string.Join(" ", reduced.Select(r => r.ToUserString()));
                context.Output.WriteLine(output);
                return b; // Return the original block (or none)
            }

            // Standard non-block printing
            context.Output.WriteLine(val.ToUserString());
            return val;
        }, 1).WithTitle("Prints a value to the output."));

        ctx.Set("prin", new Native((args, refs, context, interpreter, isTail) =>
        {
            var val = args[0];

            if (val is Block b)
            {
                var reduced = new List<Value>();
                int index = 0;
                while (index < b.Children.Count)
                {
                    reduced.Add(interpreter.Next(b, ref index, context, false));
                }

                var output = string.Join(" ", reduced.Select(r => r.ToUserString()));
                context.Output.Write(output);
                return b;
            }

            context.Output.Write(val.ToUserString());
            return val;
        }, 1).WithTitle("Prints a value to the output without a newline."));
    }
}