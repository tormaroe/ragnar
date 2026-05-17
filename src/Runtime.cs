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
        ctx.Set("none", new Word("none"));

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
        ComparisonFunctions.Add(ctx);
        LogicalFunctions.Add(ctx);
        ConversionFunctions.Add(ctx);
        ObjectFunctions.Add(ctx);
        ExitFunction.Add(ctx);

        Interop.AddInteropFunctions(ctx);
        Inspection.AddInspectionFunctions(ctx);
        OS.AddOsFunctions(ctx);
        IO.AddIoFunctions(ctx);
        IoFunctions.Add(ctx);
        Actor.AddActorFunctions(ctx);

        // System object
        var systemCtx = new Context();
        var consoleCtx = new Context();

        consoleCtx.Set("prompt", new Text(">> "));
        consoleCtx.Set("result", new Text("== "));
        consoleCtx.Set("history", new Block());

        systemCtx.Set("console", new ObjectValue(consoleCtx));
        ctx.Set("system", new ObjectValue(systemCtx));

        return ctx;
    }
}