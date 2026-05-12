namespace Ragnar.Natives;

public static class MathFunctions
{
    public static void Add(Context ctx)
    {
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

        ctx.Set("multiply", new Native((args, refs, context, interpreter) =>
        {
            // 1. Handle Integer multiplication
            if (args[0] is Integer i1 && args[1] is Integer i2)
            {
                return new Integer(i1.Number * i2.Number);
            }

            // 2. Handle Decimal (or mixed math if you want to be fancy)
            double val1 = GetDoubleValue(args[0]);
            double val2 = GetDoubleValue(args[1]);

            return new Decimal(val1 * val2);
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

        // Note: 'mul' was a duplicate of 'multiply' with slightly different implementation in Runtime.cs
        // Consolidating to use the same logic as 'multiply'
        var multiplyNative = new Native((args, refs, context, interpreter) =>
        {
            if (args[0] is Integer i1 && args[1] is Integer i2)
            {
                return new Integer(i1.Number * i2.Number);
            }

            double val1 = GetDoubleValue(args[0]);
            double val2 = GetDoubleValue(args[1]);

            return new Decimal(val1 * val2);
        }, 2);
        
        ctx.Set("mul", multiplyNative);

        ctx.Set("sub", new Native((args, refinements, _, _) =>
        {
            if (args[0] is Integer i1 && args[1] is Integer i2)
                return new Integer(i1.Number - i2.Number);

            double d1 = args[0] is Decimal dec1 ? dec1.Number : (args[0] is Integer int1 ? int1.Number : 0);
            double d2 = args[1] is Decimal dec2 ? dec2.Number : (args[1] is Integer int2 ? int2.Number : 0);
            return new Decimal(d1 - d2);
        }, 2));
    }

    // Helper to make math easier across types
    private static double GetDoubleValue(Value v) => v switch
    {
        Integer i => (double)i.Number,
        Decimal d => d.Number,
        _ => throw new Exception($"Cannot perform math on {v.GetType().Name}")
    };
}
