namespace Ragnar.Natives;

public static class SeriesFunctions
{
    public static void Add(Context ctx)
    {
        // Helper for positional access
        static Value GetAt(List<Value> items, int index) =>
            (index >= 0 && index < items.Count) ? items[index] : new Word("none");

        // first [10 20] -> 10
        ctx.Set("first", new Native((args, refinements, _, _) =>
        {
            if (args[0] is Block b) return GetAt(b.Children, 0);
            if (args[1] is Text t) return new Text(t.Content[0].ToString());
            throw new Exception("first requires a block or text.");
        }, 1));

        // second [10 20] -> 20
        ctx.Set("second", new Native((args, refinements, _, _) =>
        {
            if (args[0] is Block b) return GetAt(b.Children, 1);
            throw new Exception("second requires a block.");
        }, 1));

        // last [10 20] -> 20
        ctx.Set("last", new Native((args, refinements, _, _) =>
        {
            if (args[0] is Block b) return GetAt(b.Children, b.Children.Count - 1);
            throw new Exception("last requires a block.");
        }, 1));

        // length? [1 2 3] -> 3
        ctx.Set("length?", new Native((args, refinements, _, _) =>
        {
            if (args[0] is Block b) return new Integer(b.Children.Count);
            if (args[0] is Text t) return new Integer(t.Content.Length);
            throw new Exception("length? requires a block or text.");
        }, 1));

        // append [1 2] 3 -> [1 2 3]
        ctx.Set("append", new Native((args, refinements, _, _) =>
        {
            if (args[0] is Block b)
            {
                b.Children.Add(args[1]);
                return b; // Return the modified block
            }
            throw new Exception("append requires a block as the first argument.");
        }, 2));

        // join [base] [value]
        ctx.Set("join", new Native((args, refs, context, interpreter) =>
        {
            // For now, we'll focus on string concatenation
            // but in a full Rebol clone, join [1 2] 3 would return [1 2 3]
            string baseStr = args[0].ToUserString();
            string appendStr = args[1].ToUserString();

            return new Text(baseStr + appendStr);
        }, 2));

        // rejoin [block]
        ctx.Set("rejoin", new Native((args, refs, context, interpreter) =>
        {
            if (args[0] is not Block b)
                throw new Exception("rejoin expects a block.");

            var sb = new System.Text.StringBuilder();
            int index = 0;

            // Evaluate each expression in the block and append to string
            while (index < b.Children.Count)
            {
                var evaluated = interpreter.Next(b, ref index, context);
                sb.Append(evaluated.ToUserString());
            }

            return new Text(sb.ToString());
        }, 1));
    }
}
