using System.Runtime.InteropServices;
using Ragnar.Natives;

namespace Ragnar;

public static class Runtime
{
    public static Context CreateGlobalContext()
    {
        var ctx = new Context();

        // 1. Constants
        ctx.Set("true", new Logic(true));
        ctx.Set("false", new Logic(false));

        PrintFunction.Add(ctx);
        DoFunction.Add(ctx);
        ConditionalFunctions.Add(ctx);
        LoopFunction.Add(ctx);
        WhileFunction.Add(ctx);
        ForeachFunction.Add(ctx);
        MathFunctions.Add(ctx);
        FuncFunction.Add(ctx);
        SeriesFunctions.Add(ctx);
        BlockFunctions.Add(ctx);
        StringFunctions.Add(ctx);
        ExitFunction.Add(ctx);

        Interop.AddInteropFunctions(ctx);
        Inspection.AddInspectionFunctions(ctx);
        OS.AddOsFunctions(ctx);
        IO.AddIoFunctions(ctx);

        return ctx;
    }
}