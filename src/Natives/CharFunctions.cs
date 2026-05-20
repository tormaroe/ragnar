namespace Ragnar.Natives;

public static class CharFunctions
{
    public static void Add(Context ctx)
    {
        // char? [value]
        ctx.Set("char?", new Native((args, refs, _, _, _) =>
        {
            return new Logic(args[0] is Character);
        }, 1).WithTitle("Returns true if the value is a character."));
    }
}
