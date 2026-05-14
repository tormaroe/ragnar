namespace Ragnar.Natives;

public static class MathFunctions
{
    public static void Add(Context ctx)
    {
        // prefix versions
        var add = new Native((args, refinements, _, _) =>
        {
            if (args[0] is Integer i1 && args[1] is Integer i2)
                return new Integer(i1.Number + i2.Number);

            double d1 = GetDoubleValue(args[0]);
            double d2 = GetDoubleValue(args[1]);
            return new Decimal(d1 + d2);
        }, 2);
        ctx.Set("add", add);
        ctx.Set("+", new Op(add.Action));

        var sub = new Native((args, refinements, _, _) =>
        {
            if (args[0] is Integer i1 && args[1] is Integer i2)
                return new Integer(i1.Number - i2.Number);

            double d1 = GetDoubleValue(args[0]);
            double d2 = GetDoubleValue(args[1]);
            return new Decimal(d1 - d2);
        }, 2);
        ctx.Set("sub", sub);
        ctx.Set("-", new Op(sub.Action));

        var multiply = new Native((args, refs, context, interpreter) =>
        {
            if (args[0] is Integer i1 && args[1] is Integer i2)
                return new Integer(i1.Number * i2.Number);

            double val1 = GetDoubleValue(args[0]);
            double val2 = GetDoubleValue(args[1]);
            return new Decimal(val1 * val2);
        }, 2);
        ctx.Set("multiply", multiply);
        ctx.Set("mul", multiply);
        ctx.Set("*", new Op(multiply.Action));

        var divide = new Native((args, refs, context, interpreter) =>
        {
            double val1 = GetDoubleValue(args[0]);
            double val2 = GetDoubleValue(args[1]);
            if (val2 == 0) throw new Exception("Division by zero.");
            
            double result = val1 / val2;
            if (result == Math.Floor(result)) return new Integer((long)result);
            return new Decimal(result);
        }, 2);
        ctx.Set("divide", divide);
        ctx.Set("/", new Op(divide.Action));

        var remainder = new Native((args, refs, context, interpreter) =>
        {
            if (args[0] is Integer i1 && args[1] is Integer i2)
            {
                if (i2.Number == 0) throw new Exception("Remainder by zero.");
                return new Integer(i1.Number % i2.Number);
            }

            double val1 = GetDoubleValue(args[0]);
            double val2 = GetDoubleValue(args[1]);
            if (val2 == 0) throw new Exception("Remainder by zero.");
            return new Decimal(val1 % val2);
        }, 2);
        ctx.Set("remainder", remainder);
        ctx.Set("//", new Op(remainder.Action));
    }

    private static double GetDoubleValue(Value v) => v switch
    {
        Integer i => (double)i.Number,
        Decimal d => d.Number,
        _ => throw new Exception($"Cannot perform math on {v.GetType().Name}")
    };
}
