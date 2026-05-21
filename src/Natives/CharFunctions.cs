using System.Collections.Generic;

namespace Ragnar.Natives;

public static class CharFunctions
{
    public static void Add(Context ctx)
    {
        // char? [value]
        ctx.Set("char?", new Native((args, refs, _, _, _) =>
        {
            return new Logic(args[0] is Character);
        }, 1).WithTitle("Returns true if the value is a character."));

        // charset [spec]
        // Creates a bitset from a string (or block) of characters.
        // Usage: charset "0123456789"  => bitset matching any digit
        ctx.Set("charset", new Native((args, refs, _, _, _) =>
        {
            var chars = new HashSet<char>();
            if (args[0] is Text t)
            {
                foreach (char c in t.Content)
                    chars.Add(c);
            }
            else if (args[0] is Block b)
            {
                foreach (var v in b.Children)
                {
                    if (v is Character ch) chars.Add(ch.CharValue);
                    else if (v is Text st && st.Content.Length == 1) chars.Add(st.Content[0]);
                }
            }
            else
            {
                throw new Exception("charset requires a string or block argument.");
            }
            return new Bitset(chars);
        }, 1).WithTitle("Creates a bitset of characters for use in parse rules."));
    }
}
