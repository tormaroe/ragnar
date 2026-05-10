
namespace Ragnar;

public class Interpreter
{
    public Value Evaluate(Block block, Context context)
    {
        Value lastValue = new Word("none"); // Default return value
        int index = 0;

        // Loop through every item in the block
        while (index < block.Children.Count)
        {
            lastValue = Next(block, ref index, context);
        }

        return lastValue;
    }

    // The 'Next' method evaluates the single next expression.
    // If it's a function, it recursively evaluates its arguments.
    private Value Next(Block block, ref int index, Context context)
    {
        if (index >= block.Children.Count)
            throw new Exception("Unexpected end of block: more arguments expected.");

        Value current = block.Children[index++];

        // --- HANDLE LIT-WORD ---
        if (current is LitWord lit)
        {
            return new Word(lit.Name);
        }

        // --- HANDLE GET-WORD ---
        if (current is GetWord getWord)
        {
            // Just return the value as-is, no execution!
            return context.Get(getWord.Name);
        }

        // --- HANDLE SET-WORD ---
        if (current is SetWord setWord)
        {
            // Evaluate the NEXT expression to get the value to assign
            Value result = Next(block, ref index, context);
            context.Set(setWord.Name, result);
            return result;
        }

        // --- HANDLE REGULAR WORD (existing logic) ---
        if (current is Word word)
        {
            Value boundValue = context.Get(word.Name);

            // If the word points to a Native function, we must gather args
            if (boundValue is Native native)
            {
                List<Value> args = [];
                for (int i = 0; i < native.Arity; i++)
                {
                    // Recursively get the next complete expression for each argument
                    args.Add(Next(block, ref index, context));
                }
                return native.Action(args, [], context, this);
            }

            // --- NEW: HANDLE USER FUNCTIONS ---
            if (boundValue is Function func)
            {
                // 1. Create a local scope for this function call
                var localCtx = new Context(context); 

                // 2. Gather arguments and bind them to parameter names
                foreach (var paramName in func.Parameters)
                {
                    // Evaluate the next expression in the CALLER'S context
                    Value argValue = Next(block, ref index, context);
                    // Set it in the FUNCTION'S local context
                    localCtx.Set(paramName, argValue);
                }

                // 3. Run the body in the local context
                return Evaluate(func.Body, localCtx);
            }

            // If it's just a variable (like an Integer), return it
            return boundValue;
        }

        if (current is Path path)
        {
            // 1. Resolve the "head" of the path (look it up, don't evaluate it!)
            Value head;
            if (path.Parts[0] is Word w)
            {
                head = context.Get(w.Name);
            }
            else if (path.Parts[0] is GetWord gw)
            {
                head = context.Get(gw.Name);
            }
            else
            {
                head = path.Parts[0];
            }

            // 2. Handle Refinements for Natives or User Functions
            if (head is Native native)
            {
                var refinements = new HashSet<string>();
                foreach (var part in path.Parts.Skip(1))
                {
                    if (part is Word rw) refinements.Add(rw.Name);
                    else throw new Exception($"Invalid refinement in path: {part}");
                }

                var args = new List<Value>();
                for (int i = 0; i < native.Arity; i++)
                {
                    args.Add(Next(block, ref index, context));
                }

                return native.Action(args, refinements, context, this);
            }
            else if (head is Function func)
            {
                // We should also support refinements for user-defined functions!
                var refinements = new HashSet<string>();
                foreach (var part in path.Parts.Skip(1))
                {
                    if (part is Word rw) refinements.Add(rw.Name);
                }

                var args = new List<Value>();
                for (int i = 0; i < func.Parameters.Count; i++)
                {
                    args.Add(Next(block, ref index, context));
                }

                return ExecuteFunction(func, args, refinements, context);
            }
            
            throw new Exception($"Paths starting with {head.GetType().Name} are not yet supported.");
        }

        // Literals (Integers, Decimals, Strings, and Blocks) evaluate to themselves.
        // A Block is just data until something like the 'do' function evaluates it.
        return current;
    }

    private Value ExecuteFunction(Function func, List<Value> args, HashSet<string> refinements, Context context)
    {
        var localContext = new Context(context);
        for (int i = 0; i < func.Parameters.Count; i++)
        {
            localContext.Set(func.Parameters[i], args[i]);
        }

        // In a more advanced version, we would also set words for the refinements 
        // (e.g., setting a 'wait' word to true inside the function's context).
        
        return Evaluate(func.Body, localContext);
    }
}