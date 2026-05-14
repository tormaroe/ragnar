using System;
using System.Collections.Generic;

namespace Ragnar.Natives;

public static class ComparisonFunctions
{
    public static void Add(Context ctx)
    {
        // greater? [val1] [val2]
        var greater = new Native((args, refs, _, _) =>
        {
            double d1 = ToDouble(args[0]);
            double d2 = ToDouble(args[1]);
            return new Logic(d1 > d2);
        }, 2);
        ctx.Set("greater?", greater);
        ctx.Set(">", new Op(greater.Action));

        // less? [val1] [val2]
        var less = new Native((args, refs, _, _) =>
        {
            double d1 = ToDouble(args[0]);
            double d2 = ToDouble(args[1]);
            return new Logic(d1 < d2);
        }, 2);
        ctx.Set("less?", less);
        ctx.Set("<", new Op(less.Action));

        // equal? [val1] [val2]
        var equal = new Native((args, refs, _, _) =>
        {
            // For now, simple object equality or string representation comparison
            if (args[0] is Integer i1 && args[1] is Integer i2) return new Logic(i1.Number == i2.Number);
            if (args[0] is Decimal dec1 && args[1] is Decimal dec2) return new Logic(dec1.Number == dec2.Number);
            return new Logic(args[0].ToString() == args[1].ToString());
        }, 2);
        ctx.Set("equal?", equal);
        ctx.Set("=", new Op(equal.Action));
        ctx.Set("==", new Op(equal.Action));

        // not-equal? [val1] [val2]
        var notEqual = new Native((args, refs, _, _) =>
        {
            if (args[0] is Integer i1 && args[1] is Integer i2) return new Logic(i1.Number != i2.Number);
            if (args[0] is Decimal dec1 && args[1] is Decimal dec2) return new Logic(dec1.Number != dec2.Number);
            return new Logic(args[0].ToString() != args[1].ToString());
        }, 2);
        ctx.Set("not-equal?", notEqual);
        ctx.Set("<>", new Op(notEqual.Action));
        ctx.Set("!=", new Op(notEqual.Action));

        // greater-or-equal? [val1] [val2]
        var greaterOrEqual = new Native((args, refs, _, _) =>
        {
            double d1 = ToDouble(args[0]);
            double d2 = ToDouble(args[1]);
            return new Logic(d1 >= d2);
        }, 2);
        ctx.Set("greater-or-equal?", greaterOrEqual);
        ctx.Set(">=", new Op(greaterOrEqual.Action));

        // less-or-equal? [val1] [val2]
        var lessOrEqual = new Native((args, refs, _, _) =>
        {
            double d1 = ToDouble(args[0]);
            double d2 = ToDouble(args[1]);
            return new Logic(d1 <= d2);
        }, 2);
        ctx.Set("less-or-equal?", lessOrEqual);
        ctx.Set("<=", new Op(lessOrEqual.Action));
    }

    private static double ToDouble(Value v) => v switch
    {
        Integer i => (double)i.Number,
        Decimal d => d.Number,
        _ => throw new Exception($"Cannot compare {v.GetType().Name}")
    };
}
