namespace Ragnar.Natives;

public static class MathFunctions
{
    public static void Add(Context ctx)
    {
        // prefix versions
        var add = new Native((args, refinements, _, _, _) =>
        {
            if (args[0] is Integer i1 && args[1] is Integer i2)
                return new Integer(i1.Number + i2.Number);

            double d1 = GetDoubleValue(args[0]);
            double d2 = GetDoubleValue(args[1]);
            return new Decimal(d1 + d2);
        }, 2);
        ctx.Set("add", add);
        ctx.Set("+", new Op(add.Action));

        var sub = new Native((args, refinements, _, _, _) =>
        {
            if (args[0] is Integer i1 && args[1] is Integer i2)
                return new Integer(i1.Number - i2.Number);

            double d1 = GetDoubleValue(args[0]);
            double d2 = GetDoubleValue(args[1]);
            return new Decimal(d1 - d2);
        }, 2);
        ctx.Set("sub", sub);
        ctx.Set("-", new Op(sub.Action));

        var multiply = new Native((args, refs, context, interpreter, _) =>
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

        var divide = new Native((args, refs, context, interpreter, _) =>
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

        var remainder = new Native((args, refs, context, interpreter, _) =>
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

        var random = new Native((args, refs, context, interpreter, _) =>
        {
            if (refs.Contains("seed"))
            {
                int seedValue = (int)GetDoubleValue(args[0]);
                _rng = new Random(seedValue);
                return new Word("none");
            }

            if (args[0] is Integer i)
            {
                if (i.Number <= 0) throw new Exception("random expects a positive integer.");
                // Rebol: random 10 returns 1 to 10 inclusive
                return new Integer(_rng.Next(1, (int)i.Number + 1));
            }

            if (args[0] is Decimal d)
            {
                if (d.Number <= 0) throw new Exception("random expects a positive decimal.");
                // Rebol: random 10.0 returns 0.0 to 10.0
                return new Decimal(_rng.NextDouble() * d.Number);
            }

            throw new Exception("random expects an integer or decimal.");
        }, 1);
        ctx.Set("random", random);
    }

    private static Random _rng = new Random();

    private static double GetDoubleValue(Value v) => v switch
    {
        Integer i => (double)i.Number,
        Decimal d => d.Number,
        _ => throw new Exception($"Cannot perform math on {v.GetType().Name}")
    };
}
