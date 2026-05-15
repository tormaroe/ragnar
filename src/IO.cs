namespace Ragnar;

public class IO
{
    public static void AddIoFunctions(Context ctx)
    {
        // read %file.txt (returns string) or read/lines %file.txt (returns block)
        ctx.Set("read", new Native((args, refinements, context, interpreter, isTail) =>
        {
            string path = GetPathFromValue(args[0]);

            if (!System.IO.File.Exists(path))
                throw new Exception($"File not found: {path}");

            if (refinements.Contains("lines"))
            {
                // Read all lines and wrap each in a Text object
                var lines = System.IO.File.ReadAllLines(path);
                return new Block(lines.Select(l => new Text(l)));
            }

            return new Text(System.IO.File.ReadAllText(path));
        }, 1));

        // write %file.txt "data" or write/append %file.txt "data"
        ctx.Set("write", new Native((args, refinements, context, interpreter, isTail) =>
        {
            string path = GetPathFromValue(args[0]);
            string content = args[1].ToUserString();

            try
            {
                if (refinements.Contains("append"))
                {
                    // Note: In a production engine, we'd handle newlines more carefully here
                    System.IO.File.AppendAllText(path, content);
                }
                else
                {
                    System.IO.File.WriteAllText(path, content);
                }
                return args[1];
            }
            catch (Exception ex)
            {
                throw new Exception($"IO Error: {ex.Message}");
            }
        }, 2));
    }

    // Helper for the natives
    static string GetPathFromValue(Value val) => val switch
    {
        File f => f.Path,
        Text t => t.Content,
        _ => throw new Exception("Expected a file! or text! for the path.")
    };
}