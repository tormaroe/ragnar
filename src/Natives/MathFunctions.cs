using System;
using System.Collections.Generic;
using System.Linq;

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
        }, 2).WithTitle("Returns the sum of two values.");
        ctx.Set("add", add);
        ctx.Set("+", new Op(add.Action).WithTitle("Returns the sum of two values."));

        var sub = new Native((args, refinements, _, _, _) =>
        {
            if (args[0] is Integer i1 && args[1] is Integer i2)
                return new Integer(i1.Number - i2.Number);

            double d1 = GetDoubleValue(args[0]);
            double d2 = GetDoubleValue(args[1]);
            return new Decimal(d1 - d2);
        }, 2).WithTitle("Returns the difference between two values.");
        ctx.Set("sub", sub);
        ctx.Set("-", new Op(sub.Action).WithTitle("Returns the difference between two values."));

        var multiply = new Native((args, refs, context, interpreter, _) =>
        {
            if (args[0] is Integer i1 && args[1] is Integer i2)
                return new Integer(i1.Number * i2.Number);

            double val1 = GetDoubleValue(args[0]);
            double val2 = GetDoubleValue(args[1]);
            return new Decimal(val1 * val2);
        }, 2).WithTitle("Returns the product of two values.");
        ctx.Set("multiply", multiply);
        ctx.Set("mul", multiply);
        ctx.Set("*", new Op(multiply.Action).WithTitle("Returns the product of two values."));

        var divide = new Native((args, refs, context, interpreter, _) =>
        {
            double val1 = GetDoubleValue(args[0]);
            double val2 = GetDoubleValue(args[1]);
            if (val2 == 0) throw new Exception("Division by zero.");
            
            double result = val1 / val2;
            if (result == Math.Floor(result)) return new Integer((long)result);
            return new Decimal(result);
        }, 2).WithTitle("Returns the quotient of two values.");
        ctx.Set("divide", divide);
        ctx.Set("/", new Op(divide.Action).WithTitle("Returns the quotient of two values."));

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
        }, 2).WithTitle("Returns the remainder of division.");
        ctx.Set("remainder", remainder);
        ctx.Set("//", new Op(remainder.Action).WithTitle("Returns the remainder of division."));

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
        }, 1).WithTitle("Returns a random value.");
        ctx.Set("random", random);

        ctx.Set("abs", new Native((args, refs, context, interpreter, _) =>
        {
            if (args[0] is Integer i) return new Integer(Math.Abs(i.Number));
            if (args[0] is Decimal d) return new Decimal(Math.Abs(d.Number));
            throw new Exception("abs expects an integer or decimal.");
        }, 1).WithTitle("Returns the absolute value."));
    }

    private static Random _rng = new Random();

    private static double GetDoubleValue(Value v) => v switch
    {
        Integer i => (double)i.Number,
        Decimal d => d.Number,
        _ => throw new Exception($"Cannot perform math on {v.GetType().Name}")
    };
}
