namespace rebelly;

public static class Runtime
{
    public static Context CreateGlobalContext()
    {
        var ctx = new Context();

        // 1. Constants
        ctx.Set("true", new Logic(true));
        ctx.Set("false", new Logic(false));

        // 2. print [val]
        ctx.Set("print", new Native((args, _, _) => {
            Console.WriteLine(args[0].ToString());
            return args[0];
        }, 1));

        // 3. do [block]
        // This is the core of homoiconicity: treating data as code.
        ctx.Set("do", new Native((args, context, interpreter) => {
            if (args[0] is Block b) return interpreter.Evaluate(b, context);
            return args[0]; // If not a block, just return the value
        }, 1));

        // 4. if [condition] [block]
        ctx.Set("if", new Native((args, context, interpreter) => {
            bool isTrue = (args[0] is Logic l && l.Condition);
            if (isTrue && args[1] is Block b) {
                return interpreter.Evaluate(b, context);
            }
            return new Word("none");
        }, 2));

        // 5. equal? [val1] [val2]
        ctx.Set("equal?", new Native((args, _, _) => {
            // Simple comparison for now
            return new Logic(args[0].ToString() == args[1].ToString());
        }, 2));

        // add [val1] [val2]
        ctx.Set("add", new Native((args, _, _) => {
            if (args[0] is Integer i1 && args[1] is Integer i2)
                return new Integer(i1.Number + i2.Number);
            
            // Basic math for decimals too
            double d1 = args[0] is Decimal dec1 ? dec1.Number : (args[0] is Integer int1 ? int1.Number : 0);
            double d2 = args[1] is Decimal dec2 ? dec2.Number : (args[1] is Integer int2 ? int2.Number : 0);
            return new Decimal(d1 + d2);
        }, 2));

        ctx.Set("mul", new Native((args, _, _) => {
            if (args[0] is Integer i1 && args[1] is Integer i2)
                return new Integer(i1.Number * i2.Number);
            
            double d1 = args[0] is Decimal dec1 ? dec1.Number : (args[0] is Integer int1 ? int1.Number : 0);
            double d2 = args[1] is Decimal dec2 ? dec2.Number : (args[1] is Integer int2 ? int2.Number : 0);
            return new Decimal(d1 * d2);
        }, 2));

        ctx.Set("sub", new Native((args, _, _) => {
            if (args[0] is Integer i1 && args[1] is Integer i2)
                return new Integer(i1.Number - i2.Number);
            
            double d1 = args[0] is Decimal dec1 ? dec1.Number : (args[0] is Integer int1 ? int1.Number : 0);
            double d2 = args[1] is Decimal dec2 ? dec2.Number : (args[1] is Integer int2 ? int2.Number : 0);
            return new Decimal(d1 - d2);
        }, 2));

        // func [spec] [body]
        ctx.Set("func", new Native((args, context, interpreter) => {
            if (args[0] is not Block spec) throw new Exception("func spec must be a block.");
            if (args[1] is not Block body) throw new Exception("func body must be a block.");

            // Convert the words in the spec block into a list of strings
            var parameters = spec.Children
                .Select(v => (v as Word)?.Name ?? throw new Exception("Spec must contain words."))
                .ToList();

            return new Function(parameters, body);
        }, 2));

        // exit / quit
        var exitNative = new Native((args, _, _) => 
        {
            Console.WriteLine("Goodbye!");
            Environment.Exit(0); 
            return new Word("none"); // This line is never actually reached
        }, 0);

        ctx.Set("exit", exitNative);
        ctx.Set("quit", exitNative);

        Interop.AddInteropFunctions(ctx);

        return ctx;
    }
}