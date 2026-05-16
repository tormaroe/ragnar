namespace Ragnar.Natives;

public static class ExitFunction
{
    public static void Add(Context ctx)
    {
        // exit - return none from function
        ctx.Set("exit", new Native((args, refinements, _, _, _) => 
            throw new ReturnException(new Word("none")), 0).WithTitle("Exits the current function."));

        // quit - exit the program/repl
        ctx.Set("quit", new Native((args, refinements, _, _, _) =>
        {
            Environment.Exit(0);
            return new Word("none");
        }, 0).WithTitle("Exits the program."));
    }
}
