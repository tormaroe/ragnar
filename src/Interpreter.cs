
using System.Reflection;

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
            // 1. Start with the head
            Value currentVal = ResolvePathHead(context, path);

            // 2. Iterate through segments (starting from the second part)
            for (int i = 1; i < path.Parts.Count; i++)
            {
                var segment = path.Parts[i];

                // --- BRANCH A: It's a Function/Native ---
                // If we hit a function, the rest of the path parts are REFINEMENTS.
                if (currentVal is Native or Function)
                {
                    // Gather the rest of the path as strings
                    var refinements = new HashSet<string>();
                    for (int j = i; j < path.Parts.Count; j++)
                    {
                        if (path.Parts[j] is Word rw) refinements.Add(rw.Name);
                    }

                    // Standard argument collection and execution
                    if (currentVal is Native n)
                    {
                        var args = new List<Value>();
                        for (int k = 0; k < n.Arity; k++) args.Add(Next(block, ref index, context));
                        return n.Action(args, refinements, context, this);
                    }
                    else if (currentVal is Function f)
                    {
                        var args = new List<Value>();
                        for (int k = 0; k < f.Parameters.Count; k++) args.Add(Next(block, ref index, context));
                        return ExecuteFunction(f, args, refinements, context);
                    }
                }

                // --- BRANCH B: It's a .NET Object ---
                if (currentVal is DotNetValue dnv)
                {
                    currentVal = GetDotNetMember(dnv.Instance, segment.ToString());
                    continue;
                }

                // --- BRANCH C: It's a Block (Index Access) ---
                if (currentVal is Block b && segment is Integer idx)
                {
                    int listIdx = (int)idx.Number - 1; // Rebol is 1-indexed
                    currentVal = (listIdx >= 0 && listIdx < b.Children.Count)
                                 ? b.Children[listIdx]
                                 : new Word("none");
                    continue;
                }

                // If we get here and haven't 'continued' or 'returned', it's an error
                throw new Exception($"Cannot navigate into {currentVal.GetType().Name} with segment {segment}");
            }

            return currentVal;
        }

        // Literals (Integers, Decimals, Strings, and Blocks) evaluate to themselves.
        // A Block is just data until something like the 'do' function evaluates it.
        return current;
    }

    private static Value ResolvePathHead(Context context, Path path)
    {
        // 1. Resolve the "head" of the path (look it up, don't evaluate it!)
        // Value head;
        // if (path.Parts[0] is Word w)
        // {
        //     head = context.Get(w.Name);
        // }
        // else if (path.Parts[0] is GetWord gw)
        // {
        //     head = context.Get(gw.Name);
        // }
        // else
        // {
        //     head = path.Parts[0];
        // }

        // return head;
        var first = path.Parts[0];
        if (first is Word w)
        {
            // Try to get from context, but if not found, 
            // check if it's a .NET Type name (e.g. "System.Math")
            try { return context.Get(w.Name); }
            catch
            {
                // If word not in context, see if it's a Static Type
                try { return new DotNetValue(Interop.ResolveType(w.Name)); }
                catch { throw; } // Re-throw if it's truly not found
            }
        }
        return first;
    }

    private Value GetDotNetMember(object target, string memberName)
    {
        // If target is a Type, we look for Statics. If it's an instance, we look for Instance members.
        bool isStatic = target is Type;
        Type type = isStatic ? (Type)target : target.GetType();
        var flags = BindingFlags.Public | BindingFlags.IgnoreCase |
                    (isStatic ? BindingFlags.Static : BindingFlags.Instance);

        // Try Property
        var prop = type.GetProperty(memberName, flags);
        if (prop != null) return Interop.ToRagnarValue(prop.GetValue(isStatic ? null : target));

        // Try Field
        var field = type.GetField(memberName, flags);
        if (field != null) return Interop.ToRagnarValue(field.GetValue(isStatic ? null : target));

        throw new Exception($"Member '{memberName}' not found on {type.Name}");
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