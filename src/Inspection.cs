namespace Ragnar;

public class Inspection
{
    public static void AddInspectionFunctions(Context ctx)
    {
        // what (no arguments)
        ctx.Set("what", new Native((args, refinements, context, interpreter, isTail) => {
            var all = context.GetAllBindings();
            
            // Filter for function types
            var functions = all
                .Where(kvp => kvp.Value is Native || kvp.Value is Function)
                .OrderBy(kvp => kvp.Key);

            ctx.Output.WriteLine("\n--- Defined Functions ---");
            
            foreach (var func in functions)
            {
                string title = func.Value switch {
                    Native n => n.Title,
                    Function f => f.Title,
                    _ => ""
                };
                ctx.Output.WriteLine($"{func.Key,-15} {title}");
            }
            
            ctx.Output.WriteLine();
            return new Word("none");
        }, 0).WithTitle("Prints a list of known functions."));

        // help print or help 'print
        var helpNative = new Native((args, refinements, context, interpreter, isTail) => {
            string? wordName = args[0] switch
            {
                Word w => w.Name,
                LitWord lw => lw.Name,
                _ => null
            };

            if (wordName == null)
            {
                ctx.Output.WriteLine("Usage: help word (e.g., help add)");
                return new Word("none");
            }

            try
            {
                Value val = context.Get(wordName);
                ctx.Output.WriteLine($"\nWORD: {wordName}");
                
                if (val is Native native)
                {
                    ctx.Output.WriteLine("TYPE:  Native Function");
                    if (!string.IsNullOrEmpty(native.Title))
                        ctx.Output.WriteLine($"TITLE: {native.Title}");
                    ctx.Output.WriteLine($"ARITY: {native.Arity} arguments");
                }
                else if (val is Function func)
                {
                    ctx.Output.WriteLine("TYPE:  User-Defined Function");
                    if (!string.IsNullOrEmpty(func.Title))
                        ctx.Output.WriteLine($"TITLE: {func.Title}");
                    
                    var spec = new List<string>();
                    foreach (var p in func.MainParameters)
                    {
                        spec.Add((p.Evaluate ? "" : "'") + p.Name);
                    }
                    foreach (var r in func.Refinements)
                    {
                        spec.Add("/" + r.Name);
                        spec.AddRange(r.Args);
                    }

                    ctx.Output.WriteLine($"ARGS:  [ {string.Join(" ", spec)} ]");
                    string formattedBody = Formatter.Format(func.Body, 0, context).TrimStart();
                    ctx.Output.WriteLine($"BODY:  {formattedBody}");
                }
                else if (val is DotNetValue dnv)
                {
                    ctx.Output.WriteLine("TYPE:  .NET Wrapper");
                    ctx.Output.WriteLine($"VALUE: {dnv.Instance?.GetType().FullName ?? "null"}");
                    ctx.Output.WriteLine($"STR:   {dnv.Instance}");
                }
                else
                {
                    // For Integers, Strings, Blocks, etc.
                    string typeName = val.GetType().Name;
                    ctx.Output.WriteLine($"TYPE:  {typeName}");
                    ctx.Output.WriteLine($"VALUE: {val}");
                }
                ctx.Output.WriteLine();
            }
            catch (Exception)
            {
                ctx.Output.WriteLine($"Word '{wordName}' is not defined in this context.");
            }

            return new Word("none");
        }, 1, [false]).WithTitle("Displays information about a word.");

        ctx.Set("help", helpNative);
        ctx.Set("?", helpNative);

        // probe [value]
        ctx.Set("probe", new Native((args, refinements, _, _, isTail) => {
            // Print the literal representation (code-friendly)
            ctx.Output.WriteLine(args[0].ToString());
            
            // Return the value as-is so it can be used in expressions
            return args[0];
        }, 1).WithTitle("Prints a value in its literal form and returns it."));

        // type? [value]
        ctx.Set("type?", new Native((args, refinements, _, _, isTail) => {
            string typeName = args[0] switch {
                Integer  => "integer!",
                Decimal  => "decimal!",
                Character => "char!",
                Text     => "text!",
                Word     => "word!",
                SetWord  => "set-word!",
                GetWord  => "get-word!",
                LitWord  => "lit-word!",
                Block    => "block!",
                Logic    => "logic!",
                Function => "function!",
                Native   => "native!",
                DotNetValue => "dotnet!",
                _        => "value!"
            };
            return new Word(typeName);
        }, 1).WithTitle("Returns the type of a value."));

        // format [value]
        ctx.Set("format", new Native((args, refinements, context, interpreter, isTail) => {
            var val = args[0];
            bool isScript = refinements.Contains("script");

            if (val is Text text)
            {
                var lexer = new Lexer(text.Content);
                var tokens = lexer.Tokenize();
                var loader = new Loader();
                var block = loader.Load(tokens);
                return new Text(Formatter.FormatBlockChildren(block, 0, context));
            }

            if (val is Block blockVal && isScript)
            {
                return new Text(Formatter.FormatBlockChildren(blockVal, 0, context));
            }

            return new Text(Formatter.Format(val, 0, context));
        }, 1).WithTitle("Pretty-prints and formats a value or Ragnar code string. Supports /script for blocks."));
    }
}