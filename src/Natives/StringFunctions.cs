using System.Linq;

namespace Ragnar.Natives;

public static class StringFunctions
{
    public static void Add(Context ctx)
    {
        ctx.Set("trim", new Native((args, refinements, _, _) =>
        {
            if (args[0] is not Text t)
                throw new Exception("trim requires a string (Text).");

            string input = t.Content;

            if (refinements.Contains("all"))
            {
                return new Text(new string(input.Where(c => !char.IsWhiteSpace(c)).ToArray()));
            }

            if (refinements.Contains("lines"))
            {
                var words = input.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                return new Text(string.Join(" ", words));
            }

            bool head = refinements.Contains("head");
            bool tail = refinements.Contains("tail");

            if (head && tail)
            {
                return new Text(input.Trim());
            }
            if (head)
            {
                return new Text(input.TrimStart());
            }
            if (tail)
            {
                return new Text(input.TrimEnd());
            }

            // Default: trim head and tail
            return new Text(input.Trim());
        }, 1));

        ctx.Set("replace", new Native((args, refinements, _, _) =>
        {
            if (args[0] is not Text target) throw new Exception("replace requires a target string.");
            if (args[1] is not Text search) throw new Exception("replace requires a search string.");
            if (args[2] is not Text replacement) throw new Exception("replace requires a replacement string.");

            string input = target.Content;
            string pattern = search.Content;
            string substitute = replacement.Content;

            if (refinements.Contains("all"))
            {
                return new Text(input.Replace(pattern, substitute));
            }
            else
            {
                int index = input.IndexOf(pattern);
                if (index < 0) return target;
                return new Text(input.Remove(index, pattern.Length).Insert(index, substitute));
            }
        }, 3));

        ctx.Set("uppercase", new Native((args, refinements, _, _) =>
        {
            if (args[0] is not Text t) throw new Exception("uppercase requires a string.");
            return new Text(t.Content.ToUpperInvariant());
        }, 1));

        ctx.Set("lowercase", new Native((args, refinements, _, _) =>
        {
            if (args[0] is not Text t) throw new Exception("lowercase requires a string.");
            return new Text(t.Content.ToLowerInvariant());
        }, 1));

        ctx.Set("split", new Native((args, refinements, _, _) =>
        {
            if (args[0] is not Text t) throw new Exception("split requires a string to split.");
            if (args[1] is not Text d) throw new Exception("split requires a delimiter string.");

            var parts = t.Content.Split(new[] { d.Content }, StringSplitOptions.None);
            return new Block(parts.Select(p => new Text(p)));
        }, 2));
    }
}
