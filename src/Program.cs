namespace rebelly;

class Program
{
    static void Main(string[] args)
    {
        var interpreter = new Interpreter();
        var globalContext = Runtime.CreateGlobalContext();

        Console.WriteLine("[ Rebelly Interpreter ]");

        // 1. Process files if provided
        if (args.Length > 0)
        {
            foreach (var path in args)
            {
                RunFile(path, interpreter, globalContext);
            }
        }

        // 2. Start the REPL
        RunRepl(interpreter, globalContext);
    }

    static void RunFile(string path, Interpreter interpreter, Context context)
    {
        if (!System.IO.File.Exists(path))
        {
            Console.WriteLine($"Error: File not found '{path}'");
            return;
        }

        try
        {
            string code = System.IO.File.ReadAllText(path);
            var tokens = new Lexer(code).Tokenize();
            var root = new Loader().Load(tokens);
            interpreter.Evaluate(root, context);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in {path}: {ex.Message}");
        }
    }

    static void RunRepl(Interpreter interpreter, Context context)
    {
        Console.WriteLine("REPL Mode (type 'quit' to exit)");
        
        while (true)
        {
            Console.Write(">> ");
            string? input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
                continue;

            try
            {
                var tokens = new Lexer(input).Tokenize();
                var root = new Loader().Load(tokens);
                var result = interpreter.Evaluate(root, context);
                
                // Print the result of the last expression
                if (result != null)
                {
                    Console.WriteLine($"== {result}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}