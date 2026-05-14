namespace Ragnar;

class Program
{
    static void Main(string[] args)
    {
        var interpreter = new Interpreter();
        var globalContext = Runtime.CreateGlobalContext();

        Console.WriteLine("\n [][][][][][][][][][ RAGNAR ][][][][][][][][][]\n");

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
        
        var buffer = new System.Text.StringBuilder();

        while (true)
        {
            if (buffer.Length == 0)
            {
                Console.Write(">> ");
            }
            else
            {
                Console.Write(".. ");
            }

            string? input = Console.ReadLine();

            if (input == "quit")
                break;

            if (string.IsNullOrWhiteSpace(input) && buffer.Length == 0)
                continue;

            buffer.AppendLine(input);

            try
            {
                var code = buffer.ToString();
                var tokens = new Lexer(code).Tokenize();
                var root = new Loader().Load(tokens);
                var result = interpreter.Evaluate(root, context);
                
                // Print the result of the last expression
                if (result != null)
                {
                    Console.WriteLine($"== {result}");
                }

                buffer.Clear();
            }
            catch (IncompleteInputException)
            {
                // Wait for more input
                continue;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                buffer.Clear();
            }
        }
    }
}