using System.Runtime.InteropServices;
using Ragnar.Natives;

namespace Ragnar;

public static class Runtime
{
    private static ObjectValue? _systemObject;
    private static readonly object _systemLock = new();

    public static ObjectValue SystemObject
    {
        get
        {
            if (_systemObject == null)
            {
                lock (_systemLock)
                {
                    if (_systemObject == null)
                    {
                        var systemCtx = new Context();
                        var consoleCtx = new Context();
                        var optionsCtx = new Context();

                        consoleCtx.Set("prompt", new Text(">> "));
                        consoleCtx.Set("result", new Text("== "));
                        consoleCtx.Set("history", new Block());

                        optionsCtx.Set("args", new Block());

                        systemCtx.Set("console", new ObjectValue(consoleCtx));
                        systemCtx.Set("options", new ObjectValue(optionsCtx));
                        systemCtx.Set("actors", new Block());

                        _systemObject = new ObjectValue(systemCtx);
                    }
                }
            }
            return _systemObject;
        }
    }

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
        ErrorFunctions.Add(ctx);
        StringFunctions.Add(ctx);
        ParseFunctions.Add(ctx);
        CharFunctions.Add(ctx);
        ComparisonFunctions.Add(ctx);
        LogicalFunctions.Add(ctx);
        ConversionFunctions.Add(ctx);
        ObjectFunctions.Add(ctx);
        FunctionalFunctions.Add(ctx);
        ExitFunction.Add(ctx);

        Interop.AddInteropFunctions(ctx);
        Inspection.AddInspectionFunctions(ctx);
        OS.AddOsFunctions(ctx);
        IO.AddIoFunctions(ctx);
        IoFunctions.Add(ctx);
        Actor.AddActorFunctions(ctx);
        ZipFunctions.Add(ctx);
        Natives.GuiFunctions.Add(ctx);

        // System object
        ctx.Set("system", SystemObject);

        return ctx;
    }
}