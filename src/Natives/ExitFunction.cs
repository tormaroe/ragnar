namespace Ragnar.Natives;

public static class ExitFunction
{
    public static void Add(Context ctx)
    {
        // exit - return none from function
        ctx.Set("exit", new Native((args, refinements, _, _, _) => 
            throw new ReturnException(new Word("none")), 0).WithTitle("Exits the current function."));
    }
}
