namespace Ragnar.Natives;

public static class DoFunction
{
    public static void Add(Context ctx)
    {
        // 3. do [block] or do %file
        // This is the core of homoiconicity: treating data as code.
        ctx.Set("do", new Native((args, refinements, context, interpreter, isTail) =>
        {
            if (args[0] is Block b) return interpreter.Evaluate(b, context, isTail);
            
            if (args[0] is File f)
            {
                if (!System.IO.File.Exists(f.Path))
                    throw new Exception($"File not found: {f.Path}");
                
                string source = System.IO.File.ReadAllText(f.Path);
                var tokens = new Lexer(source).Tokenize();
                var root = new Loader().Load(tokens);
                return interpreter.Evaluate(root, context, isTail);
            }

            return args[0]; // If not a block or file, just return the value
        }, 1).WithTitle("Evaluates a block of code or a file, or returns a value."));
    }
}
