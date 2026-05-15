namespace Ragnar.Natives;

public static class ExitFunction
{
    public static void Add(Context ctx)
    {
        // exit / quit
        var exitNative = new Native((args, refinements, _, _, _) =>
        {
            ctx.Output.WriteLine("Goodbye!");
            Environment.Exit(0);
            return new Word("none"); // This line is never actually reached
        }, 0);

        ctx.Set("exit", exitNative);
        ctx.Set("quit", exitNative);
    }
}
