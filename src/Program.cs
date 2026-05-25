using System.Reflection;

namespace Ragnar;

class Program
{
    static void Main(string[] args)
    {
        var interpreter = new Interpreter();
        var globalContext = Runtime.CreateGlobalContext();
        
        new Banner("ragnar", ConsoleColor.DarkYellow)
            .AddTrailingText(2, "  RAGNAR interpreter")
            .AddTrailingText(3, $"  Version {Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)}")
            .AddTrailingText(4, "  https://github.com/tormaroe/ragnar")
            .Print();

        // Repl.PrintColors();

        RunCode(interpreter, globalContext, Mezzanine.SOURCE);

        // --- NEW: Load user startup script ---
        string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string startupScript = System.IO.Path.Combine(homeDir, ".ragnar.r");
        if (System.IO.File.Exists(startupScript))
        {
            try
            {
                RunFile(startupScript, interpreter, globalContext);
            }
            catch (Exception ex)
            {
                Repl.WriteError($"Error in startup script {startupScript}: {ex.Message}");
            }
        }

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
            // 1. Get system/console objects
            var systemObj = (ObjectValue)context.Get("system");
            var consoleObj = (ObjectValue)systemObj.Context.Get("console");

            // 2. Sync system/console/history -> repl._history
            var histVal = consoleObj.Context.Get("history");
            if (histVal is Block hb)
            {
                repl._history.Clear();
                foreach (var item in hb.Children.Skip(hb.Index))
                {
                    repl._history.Add(item.ToUserString());
                }
            }

            // 3. Determine prompt
            string prompt = ".. ";
            if (buffer.Length == 0)
            {
                var promptVal = consoleObj.Context.Get("prompt");
                if (promptVal is Block pb)
                {
                    try
                    {
                        prompt = interpreter.Evaluate(pb, context).ToUserString();
                    }
                    catch { prompt = ">> "; }
                }
                else
                {
                    prompt = promptVal.ToUserString();
                }
            }

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

                // 4. Print the result of the last expression using system/console/result
                if (result != null)
                {
                    string resPrefix = consoleObj.Context.Get("result").ToUserString();
                    Repl.WriteResult($"{resPrefix}{result}");
                }

                // 5. Re-sync from system/console/history (user might have changed it during evaluation)
                var histValAfter = consoleObj.Context.Get("history");
                if (histValAfter is Block hbAfter)
                {
                    repl._history.Clear();
                    foreach (var item in hbAfter.Children.Skip(hbAfter.Index))
                    {
                        repl._history.Add(item.ToUserString());
                    }
                }

                // 6. Add the current command to history and sync back
                repl.AddHistory(code.TrimEnd('\r', '\n'));
                
                if (histValAfter is Block chb)
                {
                    chb.Children.Clear();
                    chb.Index = 0;
                    foreach (var h in repl._history)
                    {
                        chb.Children.Add(new Text(h));
                    }
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
                Repl.WriteError($"Error: {ex.Message}");
                buffer.Clear();
            }
        }
    }
}
