using System.Runtime.InteropServices;

namespace Ragnar;

public static class Runtime
{
    public static Context CreateGlobalContext()
    {
        var ctx = new Context();

        // 1. Constants
        ctx.Set("true", new Logic(true));
        ctx.Set("false", new Logic(false));

        // 2. print [val]
        ctx.Set("print", new Native((args, refs, context, interpreter) => {
            var val = args[0];

            if (val is Block b)
            {
                // 1. Reduce the block (evaluate all the 'add', 'now/year', etc.)
                var reduced = new List<Value>();
                int index = 0;
                while (index < b.Children.Count)
                {
                    reduced.Add(interpreter.Next(b, ref index, context));
                }

                // 2. Join the results with spaces and print
                var output = string.Join(" ", reduced.Select(r => r.ToUserString()));
                context.Output.WriteLine(output);
                return b; // Return the original block (or none)
            }

            // Standard non-block printing
            context.Output.WriteLine(val.ToUserString());
            return val;
        }, 1));

        // 3. do [block]
        // This is the core of homoiconicity: treating data as code.
        ctx.Set("do", new Native((args, refinements, context, interpreter) =>
        {
            if (args[0] is Block b) return interpreter.Evaluate(b, context);
            return args[0]; // If not a block, just return the value
        }, 1));

        // 4. if [condition] [block]
        ctx.Set("if", new Native((args, refinements, context, interpreter) =>
        {
            bool isTrue = (args[0] is Logic l && l.Condition);
            if (isTrue && args[1] is Block b)
            {
                return interpreter.Evaluate(b, context);
            }
            return new Word("none");
        }, 2));

        // loop 5 [ print "Hello" ]
        ctx.Set("loop", new Native((args, refinements, context, interpreter) =>
        {
            if (args[0] is Integer count && args[1] is Block body)
            {
                Value lastResult = new Word("none");
                for (long i = 0; i < count.Number; i++)
                {
                    lastResult = interpreter.Evaluate(body, context);
                }
                return lastResult;
            }
            throw new Exception("loop usage: loop [integer] [block]");
        }, 2));

        // while [ condition-block ] [ body-block ]
        ctx.Set("while", new Native((args, refinements, context, interpreter) =>
        {
            if (args[0] is Block condition && args[1] is Block body)
            {
                Value lastResult = new Word("none");

                // Keep evaluating the condition block. 
                // If it returns Logic(true), run the body.
                while (true)
                {
                    Value condResult = interpreter.Evaluate(condition, context);
                    if (condResult is Logic l && l.Condition)
                    {
                        lastResult = interpreter.Evaluate(body, context);
                    }
                    else
                    {
                        break;
                    }
                }
                return lastResult;
            }
            throw new Exception("while usage: while [condition-block] [body-block]");
        }, 2));

        // foreach line lines [ print line ]
        ctx.Set("foreach", new Native((args, refinements, context, interpreter) =>
        {
            if (args[0] is Word word && args[1] is Block series && args[2] is Block body)
            {
                Value lastResult = new Word("none");

                foreach (var item in series.Children)
                {
                    // 1. Set the loop variable in the current context
                    context.Set(word.Name, item);

                    // 2. Evaluate the body
                    lastResult = interpreter.Evaluate(body, context);
                }

                return lastResult;
            }
            throw new Exception("foreach usage: foreach word series block");
        }, 3));

        // 5. equal? [val1] [val2]
        ctx.Set("equal?", new Native((args, refinements, _, _) =>
        {
            // Simple comparison for now
            return new Logic(args[0].ToString() == args[1].ToString());
        }, 2));

        // add [val1] [val2]
        ctx.Set("add", new Native((args, refinements, _, _) =>
        {
            if (args[0] is Integer i1 && args[1] is Integer i2)
                return new Integer(i1.Number + i2.Number);

            // Basic math for decimals too
            double d1 = args[0] is Decimal dec1 ? dec1.Number : (args[0] is Integer int1 ? int1.Number : 0);
            double d2 = args[1] is Decimal dec2 ? dec2.Number : (args[1] is Integer int2 ? int2.Number : 0);
            return new Decimal(d1 + d2);
        }, 2));

        ctx.Set("greater?", new Native((args, refinements, _, _) =>
        {
            if (args[0] is Integer i1 && args[1] is Integer i2)
                return new Logic(i1.Number > i2.Number);
            return new Logic(false);
        }, 2));

        ctx.Set("less?", new Native((args, refinements, _, _) =>
        {
            if (args[0] is Integer i1 && args[1] is Integer i2)
                return new Logic(i1.Number < i2.Number);
            return new Logic(false);
        }, 2));

        ctx.Set("mul", new Native((args, refinements, _, _) =>
        {
            if (args[0] is Integer i1 && args[1] is Integer i2)
                return new Integer(i1.Number * i2.Number);

            double d1 = args[0] is Decimal dec1 ? dec1.Number : (args[0] is Integer int1 ? int1.Number : 0);
            double d2 = args[1] is Decimal dec2 ? dec2.Number : (args[1] is Integer int2 ? int2.Number : 0);
            return new Decimal(d1 * d2);
        }, 2));

        ctx.Set("sub", new Native((args, refinements, _, _) =>
        {
            if (args[0] is Integer i1 && args[1] is Integer i2)
                return new Integer(i1.Number - i2.Number);

            double d1 = args[0] is Decimal dec1 ? dec1.Number : (args[0] is Integer int1 ? int1.Number : 0);
            double d2 = args[1] is Decimal dec2 ? dec2.Number : (args[1] is Integer int2 ? int2.Number : 0);
            return new Decimal(d1 - d2);
        }, 2));

        // func [spec] [body]
        ctx.Set("func", new Native((args, refinements, context, interpreter) =>
        {
            if (args[0] is not Block spec) throw new Exception("func spec must be a block.");
            if (args[1] is not Block body) throw new Exception("func body must be a block.");

            // Convert the words in the spec block into a list of strings
            var parameters = spec.Children
                .Select(v => (v as Word)?.Name ?? throw new Exception("Spec must contain words."))
                .ToList();

            return new Function(parameters, body);
        }, 2));

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

        ctx.Set("reduce", new Native((args, refs, context, interpreter) =>
        {
            if (args[0] is not Block inputBlock)
                throw new Exception("reduce expects a block.");

            var results = new List<Value>();
            int index = 0;

            // We keep calling Next until we've exhausted the block
            while (index < inputBlock.Children.Count)
            {
                results.Add(interpreter.Next(inputBlock, ref index, context));
            }

            return new Block(results);
        }, 1));

        // exit / quit
        var exitNative = new Native((args, refinements, _, _) =>
        {
            ctx.Output.WriteLine("Goodbye!");
            Environment.Exit(0);
            return new Word("none"); // This line is never actually reached
        }, 0);

        ctx.Set("exit", exitNative);
        ctx.Set("quit", exitNative);

        Interop.AddInteropFunctions(ctx);
        Inspection.AddInspectionFunctions(ctx);
        OS.AddOsFunctions(ctx);
        IO.AddIoFunctions(ctx);

        return ctx;
    }
}