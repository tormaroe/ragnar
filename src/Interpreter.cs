
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

    // The 'Next' method evaluates the single next expression, including infix operators.
    public Value Next(Block block, ref int index, Context context)
    {
        Value left = NextExpression(block, ref index, context);

        // Greedy infix lookahead (left-associative)
        while (index < block.Children.Count)
        {
            Value nextToken = block.Children[index];
            if (nextToken is Word w && context.TryGet(w.Name, out Value? v) && v is Op op)
            {
                index++; // consume the Op word
                Value right = NextExpression(block, ref index, context);
                left = op.Action([left, right], [], context, this);
            }
            else
            {
                break;
            }
        }

        return left;
    }

    public Value NextExpression(Block block, ref int index, Context context)
    {
        if (index >= block.Children.Count)
            throw new Exception("Unexpected end of block: more arguments expected.");

        Value current = block.Children[index++];

        // --- PAREN EVALUATION ---
        if (current is Paren p)
        {
            return Evaluate(p, context);
        }

        // --- HANDLE LIT-WORD ---
        if (current is LitWord lit)
        {
            return new Word(lit.Name);
        }

        // --- HANDLE GET-WORD ---
        if (current is GetWord getWord)
        {
            return context.Get(getWord.Name);
        }

        // --- HANDLE SET-WORD ---
        if (current is SetWord setWord)
        {
            Value result = Next(block, ref index, context);
            context.Set(setWord.Name, result);
            return result;
        }

        // --- HANDLE REGULAR WORD ---
        if (current is Word word)
        {
            Value boundValue = context.Get(word.Name);

            if (boundValue is Native native)
            {
                List<Value> args = [];
                for (int i = 0; i < native.Arity; i++)
                {
                    if (native.EvalArgs[i])
                    {
                        // Function arguments use Next to allow them to be greedy
                        // for infix operators (giving infix higher priority than prefix)
                        args.Add(Next(block, ref index, context));
                    }
                    else
                    {
                        if (index >= block.Children.Count) throw new Exception("Argument missing.");
                        args.Add(block.Children[index++]);
                    }
                }
                return native.Action(args, [], context, this);
            }

            if (boundValue is Function func)
            {
                var localCtx = new Context(context);
                foreach (var paramName in func.Parameters)
                {
                    Value argValue = Next(block, ref index, context);
                    localCtx.Set(paramName, argValue);
                }
                
                try
                {
                    return Evaluate(func.Body, localCtx);
                }
                catch (ReturnException ex)
                {
                    return ex.Value;
                }
            }

            return boundValue;
        }

        if (current is SetPath setPath)
        {
            Value container = ResolvePathHead(context, setPath);
            for (int i = 1; i < setPath.Parts.Count - 1; i++)
            {
                container = NavigatePath(container, setPath.Parts[i]);
            }
            Value valueToSet = Next(block, ref index, context);
            Value lastSegment = setPath.Parts.Last();

            if (container is Block b && lastSegment is Integer idx)
            {
                int listIdx = (int)idx.Number - 1;
                if (listIdx >= 0 && listIdx < b.Children.Count)
                {
                    b.Children[listIdx] = valueToSet;
                    return valueToSet;
                }
            }
            else if (container is DotNetValue dnv)
            {
                Interop.SetDotNetMember(dnv.Instance!, lastSegment.ToString(), valueToSet);
                return valueToSet;
            }

            throw new Exception($"Cannot set {lastSegment} on {container.GetType().Name}");
        }

        if (current is Path path)
        {
            Value currentVal = ResolvePathHead(context, path);
            for (int i = 1; i < path.Parts.Count; i++)
            {
                var segment = path.Parts[i];

                if (currentVal is Native or Function)
                {
                    var refinements = new HashSet<string>();
                    for (int j = i; j < path.Parts.Count; j++)
                    {
                        if (path.Parts[j] is Word rw) refinements.Add(rw.Name);
                    }

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

                if (currentVal is DotNetValue dnv)
                {
                    currentVal = GetDotNetMember(dnv.Instance, segment.ToString());
                    continue;
                }

                if (currentVal is Block b && segment is Integer idx)
                {
                    int listIdx = (int)idx.Number - 1;
                    currentVal = (listIdx >= 0 && listIdx < b.Children.Count)
                                 ? b.Children[listIdx]
                                 : new Word("none");
                    continue;
                }

                throw new Exception($"Cannot navigate into {currentVal.GetType().Name} with segment {segment}");
            }

            return currentVal;
        }

        return current;
    }

    private Value NavigatePath(Value container, Value segment)
    {
        // 1. Handle .NET Instance or Static Type access
        if (container is DotNetValue dnv)
        {
            return GetDotNetMember(dnv.Instance, segment.ToString());
        }

        // 2. Handle Block index access (e.g., b/1)
        if (container is Block b && segment is Integer idx)
        {
            int listIdx = (int)idx.Number - 1; // Ragnar is 1-indexed
            return (listIdx >= 0 && listIdx < b.Children.Count)
                ? b.Children[listIdx]
                : new Word("none");
        }

        // 3. Handle Word-based navigation for nested Ragnar structures (like Objects)
        // If you haven't implemented Ragnar Objects yet, this will be your future hook.

        throw new Exception($"Cannot navigate into {container.GetType().Name} with segment {segment}");
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

    private static Value GetDotNetMember(object? target, string memberName)
    {
        if (target == null) throw new Exception("Cannot access member on null object.");

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

        try
        {
            return Evaluate(func.Body, localContext);
        }
        catch (ReturnException ex)
        {
            return ex.Value;
        }
    }
}