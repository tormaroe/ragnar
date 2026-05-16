using System;
using System.Linq;
using System.Collections.Generic;

namespace Ragnar.Natives;

public static class IoFunctions
{
    public static void Add(Context ctx)
    {
        // input
        // Reads a line from the console without a prompt.
        ctx.Set("input", new Native((args, refinements, context, interpreter, isTail) =>
        {
            var repl = new Repl();
            return new Text(repl.ReadLine(""));
        }, 0).WithTitle("Reads a line of text from the console."));
    }
}
