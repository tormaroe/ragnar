namespace Ragnar;

public class IO
{
    private static string ReadTextWithShare(string path)
    {
        using var fs = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
        using var sr = new System.IO.StreamReader(fs, System.Text.Encoding.UTF8);
        return sr.ReadToEnd();
    }

    private static List<string> ReadLinesWithShare(string path)
    {
        var lines = new List<string>();
        using var fs = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
        using var sr = new System.IO.StreamReader(fs, System.Text.Encoding.UTF8);
        string? line;
        while ((line = sr.ReadLine()) != null)
        {
            lines.Add(line);
        }
        return lines;
    }

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
                var lines = ReadLinesWithShare(path);
                return new Block(lines.Select(l => new Text(l)));
            }

            return new Text(ReadTextWithShare(path));
        }, 1).WithTitle("Reads the content of a file.").WithRefinements("lines"));

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
        }, 2).WithTitle("Writes content to a file.").WithRefinements("append"));

        // load %file or load "1 2 3"
        ctx.Set("load", new Native((args, refinements, context, interpreter, isTail) =>
        {
            string source;
            if (args[0] is File f)
            {
                if (!System.IO.File.Exists(f.Path))
                    throw new Exception($"File not found: {f.Path}");
                source = ReadTextWithShare(f.Path);
            }
            else if (args[0] is Text t)
            {
                source = t.Content;
            }
            else
            {
                throw new Exception("load expects a file! or text! source.");
            }

            var tokens = new Lexer(source).Tokenize();
            var root = new Loader().Load(tokens);

            if (root.Children.Count == 1) return root.Children[0];
            return root;
        }, 1).WithTitle("Loads Ragnar data from a file or string."));

        // save %file value or save text-value value
        ctx.Set("save", new Native((args, refinements, context, interpreter, isTail) =>
        {
            Value target = args[0];
            Value val = args[1];
            string serialized = val.ToString();

            if (target is File f)
            {
                System.IO.File.WriteAllText(f.Path, serialized);
            }
            else if (target is Text t)
            {
                t.Content = serialized;
            }
            else
            {
                throw new Exception("save expects a file! or text! destination.");
            }

            return val;
        }, 2).WithTitle("Saves a value to a file or string."));
    }

    // Helper for the natives
    static string GetPathFromValue(Value val) => val switch
    {
        File f => f.Path,
        Text t => t.Content,
        _ => throw new Exception("Expected a file! or text! for the path.")
    };
}