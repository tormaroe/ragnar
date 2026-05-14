namespace Ragnar.Natives;

public static class SeriesFunctions
{
    public static void Add(Context ctx)
    {
        // Helper for positional access relative to series index
        static Value GetAt(Series s, int offset)
        {
            int target = s.Index + offset;
            if (s is Block b)
            {
                return (target >= 0 && target < b.Children.Count) ? b.Children[target] : new Word("none");
            }
            if (s is Text t)
            {
                return (target >= 0 && target < t.Content.Length) ? new Text(t.Content[target].ToString()) : new Word("none");
            }
            return new Word("none");
        }

        // first [10 20] -> 10
        ctx.Set("first", new Native((args, refinements, _, _) =>
        {
            if (args[0] is Series s) return GetAt(s, 0);
            throw new Exception("first requires a series.");
        }, 1));

        // second [10 20] -> 20
        ctx.Set("second", new Native((args, refinements, _, _) =>
        {
            if (args[0] is Series s) return GetAt(s, 1);
            throw new Exception("second requires a series.");
        }, 1));

        // last [10 20] -> 20
        ctx.Set("last", new Native((args, refinements, _, _) =>
        {
            if (args[0] is Series s)
            {
                if (s is Block b) return GetAt(s, b.Children.Count - s.Index - 1);
                if (s is Text t) return GetAt(s, t.Content.Length - s.Index - 1);
            }
            throw new Exception("last requires a series.");
        }, 1));

        // length? [1 2 3] -> 3
        ctx.Set("length?", new Native((args, refinements, _, _) =>
        {
            if (args[0] is Series s) return new Integer(s.Length);
            throw new Exception("length? requires a series.");
        }, 1));

        // find [series] [value]
        ctx.Set("find", new Native((args, refinements, _, _) =>
        {
            if (args[0] is not Series s) throw new Exception("find requires a series.");
            Value target = args[1];

            if (s is Text t)
            {
                string search = target is Text targetText ? targetText.Content : target.ToUserString();
                int pos = t.Content.IndexOf(search, t.Index);
                if (pos >= 0) return t.At(pos);
                return new Word("none");
            }

            if (s is Block b)
            {
                string targetStr = target.ToString();
                for (int i = s.Index; i < b.Children.Count; i++)
                {
                    if (b.Children[i].ToString() == targetStr)
                    {
                        return b.At(i);
                    }
                }
                return new Word("none");
            }

            return new Word("none");
        }, 2));

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
            int index = b.Index;

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
