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
    }
}
