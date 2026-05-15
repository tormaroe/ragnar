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
        }, 0));

        // ask "Question?"
        // Prompts the user and returns their input.
        ctx.Set("ask", new Native((args, refinements, context, interpreter, isTail) =>
        {
            string prompt = args[0].ToUserString();
            var repl = new Repl();
            return new Text(repl.ReadLine(prompt));
        }, 1));

        // confirm "Are you sure?"
        // confirm/with "Continue?" [ "y" "n" ]
        // confirm/with "Choice?" [ "a" "b" "c" ]
        // Returns logic! (true/false) for 2 options, or the selected value for > 2 options.
        ctx.Set("confirm", new Native((args, refinements, context, interpreter, isTail) =>
        {
            string question = args[0].ToUserString();
            List<Value> options = new List<Value> { new Text("y"), new Text("n") };

            if (refinements.Contains("with"))
            {
                if (args.Count < 2 || args[1] is not Block optionsBlock || optionsBlock.Children.Count < 2)
                {
                    throw new Exception("confirm/with requires a block with at least two options.");
                }
                options = optionsBlock.Children;
            }

            var optionStrings = options.Select(o => o.ToUserString().ToLower()).ToList();
            string prompt = $"{question} ({string.Join("/", options.Select(o => o.ToUserString()))}) ";
            var repl = new Repl();
            
            while (true)
            {
                string input = repl.ReadLine(prompt).Trim().ToLower();
                int index = optionStrings.IndexOf(input);
                
                if (index != -1) 
                {
                    // For exactly 2 options, we return logic! (true for first, false for second)
                    if (options.Count == 2)
                    {
                        return new Logic(index == 0);
                    }
                    // For more than 2, return the value itself
                    return options[index];
                }
                
                Console.ForegroundColor = ReplConfig.ErrorColor;
                Console.WriteLine($"Please enter one of: {string.Join(", ", optionStrings)}.");
                Console.ResetColor();
            }
        }, 2));
    }
}
