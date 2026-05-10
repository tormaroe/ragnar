namespace rebelly;

public class Inspection
{
    public static void AddInspectionFunctions(Context ctx)
    {
        // what (no arguments)
        ctx.Set("what", new Native((args, context, interpreter) => {
            var all = context.GetAllBindings();
            
            // Filter for function types
            var functions = all
                .Where(kvp => kvp.Value is Native || kvp.Value is Function)
                .OrderBy(kvp => kvp.Key);

            ctx.Output.WriteLine("\n--- Defined Functions ---");
            
            // Basic columns for readability
            int count = 0;
            foreach (var func in functions)
            {
                string typeTag = func.Value is Native ? "[native]" : "[func]";
                ctx.Output.Write($"{func.Key,-15} {typeTag,-10}");
                
                count++;
                if (count % 2 == 0) ctx.Output.WriteLine(); // Print 2 per line
            }
            
            ctx.Output.WriteLine("\n");
            return new Word("none");
        }, 0));

        // help 'print
        ctx.Set("help", new Native((args, context, interpreter) => {
            // We expect a Word (e.g., help print)
            if (args[0] is not Word w)
            {
                ctx.Output.WriteLine("Usage: help word (e.g., help add)");
                return new Word("none");
            }

            try
            {
                Value val = context.Get(w.Name);
                ctx.Output.WriteLine($"\nWORD: {w.Name}");
                
                if (val is Native native)
                {
                    ctx.Output.WriteLine("TYPE:  Native Function");
                    ctx.Output.WriteLine($"ARITY: {native.Arity} arguments");
                }
                else if (val is Function func)
                {
                    ctx.Output.WriteLine("TYPE:  User-Defined Function");
                    ctx.Output.WriteLine($"ARGS:  [ {string.Join(" ", func.Parameters)} ]");
                    ctx.Output.WriteLine($"BODY:  {func.Body}");
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
                ctx.Output.WriteLine($"Word '{w.Name}' is not defined in this context.");
            }

            return new Word("none");
        }, 1));

        // probe [value]
        ctx.Set("probe", new Native((args, _, _) => {
            // Print the literal representation (code-friendly)
            ctx.Output.WriteLine(args[0].ToString());
            
            // Return the value as-is so it can be used in expressions
            return args[0];
        }, 1));

        // type? [value]
        ctx.Set("type?", new Native((args, _, _) => {
            string typeName = args[0] switch {
                Integer  => "integer!",
                Decimal  => "decimal!",
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
        }, 1));
    }
}