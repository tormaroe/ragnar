using System.Globalization;

namespace Ragnar.Natives;

public static class ConversionFunctions
{
    public static void Add(Context ctx)
    {
        // to-integer [value]
        ctx.Set("to-integer", new Native((args, refs, _, _, isTail) =>
        {
            if (args[0] is Integer i) return i;
            if (args[0] is Decimal d) return new Integer((long)d.Number);
            if (args[0] is Character c) return new Integer(c.CharValue);
            if (args[0] is Text t)
            {
                if (long.TryParse(t.Content.Trim(), out long val)) return new Integer(val);
            }
            throw new Exception($"Cannot convert '{args[0].ToUserString()}' to integer.");
        }, 1).WithTitle("Converts a value to an integer."));

        // to-decimal [value]
        ctx.Set("to-decimal", new Native((args, refs, _, _, isTail) =>
        {
            if (args[0] is Decimal d) return d;
            if (args[0] is Integer i) return new Decimal((double)i.Number);
            if (args[0] is Text t)
            {
                if (double.TryParse(t.Content.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double val)) return new Decimal(val);
            }
            throw new Exception($"Cannot convert '{args[0].ToUserString()}' to decimal.");
        }, 1).WithTitle("Converts a value to a decimal."));

        // to-string [value]
        ctx.Set("to-string", new Native((args, refs, _, _, isTail) =>
        {
            return new Text(args[0].ToUserString());
        }, 1).WithTitle("Converts a value to a string."));

        // to-file [value]
        ctx.Set("to-file", new Native((args, refs, _, _, isTail) =>
        {
            if (args[0] is File f) return f;
            return new File(args[0].ToUserString());
        }, 1).WithTitle("Converts a value to a file path."));

        // to-char [value]
        ctx.Set("to-char", new Native((args, refs, _, _, _) =>
        {
            if (args[0] is Character c) return c;
            if (args[0] is Integer i)
            {
                try
                {
                    return new Character(Convert.ToChar(i.Number));
                }
                catch
                {
                    throw new Exception($"Integer value {i.Number} is not a valid character code point.");
                }
            }
            if (args[0] is Text t)
            {
                if (t.Content.Length > 0 && t.Index >= 0 && t.Index < t.Content.Length)
                {
                    return new Character(t.Content[t.Index]);
                }
                throw new Exception("Cannot convert empty string to character.");
            }
            throw new Exception($"Cannot convert '{args[0].ToUserString()}' to character.");
        }, 1).WithTitle("Converts a value to a character."));

        // mold [value] /only
        ctx.Set("mold", new Native((args, refinements, _, _, _) =>
        {
            Value val = args[0];
            bool only = refinements.Contains("only");

            if (only && val is Block b)
            {
                return new Text(string.Join(" ", b.Children.Skip(b.Index).Select(c => c.ToString())));
            }

            return new Text(val.ToString());
        }, 1).WithTitle("Converts a value to its Ragnar source representation."));

        // to-record [value]
        ctx.Set("to-record", new Native((args, refs, _, _, _) =>
        {
            if (args[0] is Record r) return r;
            if (args[0] is Block b)
            {
                if (b.Length % 2 != 0)
                    throw new Exception("to-record requires a block with an even number of elements.");
                return new Record(b.Children, b.Index);
            }
            throw new Exception($"Cannot convert '{args[0].ToUserString()}' to record.");
        }, 1).WithTitle("Converts a block to a record. The block must have an even number of elements."));

        // to-word [value]
        ctx.Set("to-word", new Native((args, refs, _, _, _) =>
        {
            if (args[0] is Word w) return w;
            if (args[0] is LitWord lw) return new Word(lw.Name);
            if (args[0] is SetWord sw) return new Word(sw.Name);
            if (args[0] is GetWord gw) return new Word(gw.Name);
            return new Word(args[0].ToUserString());
        }, 1).WithTitle("Converts a value to a word."));

        // to-set-word [value]
        ctx.Set("to-set-word", new Native((args, refs, _, _, _) =>
        {
            if (args[0] is SetWord sw) return sw;
            if (args[0] is Word w) return new SetWord(w.Name);
            if (args[0] is LitWord lw) return new SetWord(lw.Name);
            if (args[0] is GetWord gw) return new SetWord(gw.Name);
            return new SetWord(args[0].ToUserString());
        }, 1).WithTitle("Converts a value to a set-word."));
    }
}
