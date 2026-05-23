using System;
using System.Collections.Generic;

namespace Ragnar.Natives;

public static class ComparisonFunctions
{
    public static void Add(Context ctx)
    {
        // greater? [val1] [val2]
        var greater = new Native((args, refs, _, _, isTail) =>
        {
            double d1 = ToDouble(args[0]);
            double d2 = ToDouble(args[1]);
            return new Logic(d1 > d2);
        }, 2).WithTitle("Returns true if the first value is greater than the second.");
        ctx.Set("greater?", greater);
        ctx.Set(">", new Op(greater.Action).WithTitle("Returns true if the first value is greater than the second."));

        // less? [val1] [val2]
        var less = new Native((args, refs, _, _, isTail) =>
        {
            double d1 = ToDouble(args[0]);
            double d2 = ToDouble(args[1]);
            return new Logic(d1 < d2);
        }, 2).WithTitle("Returns true if the first value is less than the second.");
        ctx.Set("less?", less);
        ctx.Set("<", new Op(less.Action).WithTitle("Returns true if the first value is less than the second."));

        // equal? [val1] [val2]
        var equal = new Native((args, refs, _, _, isTail) =>
        {
            // For now, simple object equality or string representation comparison
            if (args[0] is Integer i1 && args[1] is Integer i2) return new Logic(i1.Number == i2.Number);
            if (args[0] is Decimal dec1 && args[1] is Decimal dec2) return new Logic(dec1.Number == dec2.Number);
            if (args[0] is DotNetValue dnv1 && args[1] is DotNetValue dnv2)
            {
                if (dnv1.Instance == null && dnv2.Instance == null) return new Logic(true);
                if (dnv1.Instance == null || dnv2.Instance == null) return new Logic(false);
                return new Logic(dnv1.Instance.Equals(dnv2.Instance));
            }
            return new Logic(args[0].ToString() == args[1].ToString());
        }, 2).WithTitle("Returns true if the values are equal.");
        ctx.Set("equal?", equal);
        ctx.Set("=", new Op(equal.Action).WithTitle("Returns true if the values are equal."));
        ctx.Set("==", new Op(equal.Action).WithTitle("Returns true if the values are equal."));

        // not-equal? [val1] [val2]
        var notEqual = new Native((args, refs, _, _, isTail) =>
        {
            if (args[0] is Integer i1 && args[1] is Integer i2) return new Logic(i1.Number != i2.Number);
            if (args[0] is Decimal dec1 && args[1] is Decimal dec2) return new Logic(dec1.Number != dec2.Number);
            if (args[0] is DotNetValue dnv1 && args[1] is DotNetValue dnv2)
            {
                if (dnv1.Instance == null && dnv2.Instance == null) return new Logic(false);
                if (dnv1.Instance == null || dnv2.Instance == null) return new Logic(true);
                return new Logic(!dnv1.Instance.Equals(dnv2.Instance));
            }
            return new Logic(args[0].ToString() != args[1].ToString());
        }, 2).WithTitle("Returns true if the values are not equal.");
        ctx.Set("not-equal?", notEqual);
        ctx.Set("<>", new Op(notEqual.Action).WithTitle("Returns true if the values are not equal."));
        ctx.Set("!=", new Op(notEqual.Action).WithTitle("Returns true if the values are not equal."));

        // greater-or-equal? [val1] [val2]
        var greaterOrEqual = new Native((args, refs, _, _, isTail) =>
        {
            double d1 = ToDouble(args[0]);
            double d2 = ToDouble(args[1]);
            return new Logic(d1 >= d2);
        }, 2).WithTitle("Returns true if the first value is greater than or equal to the second.");
        ctx.Set("greater-or-equal?", greaterOrEqual);
        ctx.Set(">=", new Op(greaterOrEqual.Action).WithTitle("Returns true if the first value is greater than or equal to the second."));

        // less-or-equal? [val1] [val2]
        var lessOrEqual = new Native((args, refs, _, _, isTail) =>
        {
            double d1 = ToDouble(args[0]);
            double d2 = ToDouble(args[1]);
            return new Logic(d1 <= d2);
        }, 2).WithTitle("Returns true if the first value is less than or equal to the second.");
        ctx.Set("less-or-equal?", lessOrEqual);
        ctx.Set("<=", new Op(lessOrEqual.Action).WithTitle("Returns true if the first value is less than or equal to the second."));
    }

    private static double ToDouble(Value v) => v switch
    {
        Integer i => (double)i.Number,
        Decimal d => d.Number,
        _ => throw new Exception($"Cannot compare {v.GetType().Name}")
    };
}
