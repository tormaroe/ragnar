namespace Ragnar;

class Program
{
    static void Main(string[] args)
    {
        var interpreter = new Interpreter();
        var globalContext = Runtime.CreateGlobalContext();
        
        Repl.Write("""
                 ___                                                                
                /___\                                                 
               (|0 0|)                                                    
             __/{\U/}\_ ___/vvv                                                
            / \  {~}   / _|_P|                                                 
            | /\  ~   /_/   ||                                                 
            |_| (____)      ||                       
            \_]/______\  /\_||_/\ 
               _\_||_/_ |] _||_ [|            
              (_,_||_,_) \/ [] \/
            """, ConsoleColor.Blue);

        Repl.WritePrint("  RAGNAR interpreter", newline: true);
        Repl.WritePrint("    https://github.com/tormaroe/ragnar\n", newline: true);

        RunCode(interpreter, globalContext, Mezzanine.SOURCE);

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
            Repl.WriteError($"Error: File not found '{path}'");
            return;
        }

        try
        {
            string code = System.IO.File.ReadAllText(path);
            RunCode(interpreter, context, code);
        }
        catch (Exception ex)
        {
            Repl.WriteError($"Error in {path}: {ex.Message}");
        }
    }

    private static void RunCode(Interpreter interpreter, Context context, string code)
    {
        var tokens = new Lexer(code).Tokenize();
        var root = new Loader().Load(tokens);
        interpreter.Evaluate(root, context);
    }

    static void RunRepl(Interpreter interpreter, Context context)
    {
        Repl.WritePrompt("REPL Mode (type 'quit' to exit)", newline: true);

        // Wrap the output in a colored writer for 'print' statements
        context.Output = new ColoredTextWriter(Console.Out, ReplConfig.PrinterColor);

        var repl = new Repl();
        var buffer = new System.Text.StringBuilder();

        while (true)
        {
            string prompt = buffer.Length == 0 ? ">> " : ".. ";
            string input = repl.ReadLine(prompt);

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
                    Repl.WriteResult($"== {result}");
                }

                // Add the successfully evaluated block to history
                repl.AddHistory(code.TrimEnd('\r', '\n'));
                buffer.Clear();
            }
            catch (IncompleteInputException)
            {
                // Wait for more input
                continue;
            }
            catch (Exception ex)
            {
                Repl.WriteError($"Error: {ex.Message}");
                buffer.Clear();
            }
        }
    }
}