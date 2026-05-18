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
    }
}
