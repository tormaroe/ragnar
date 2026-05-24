
using System.Reflection;

namespace Ragnar;

public class Interpreter
{
    public Value Evaluate(Block block, Context context, bool isTail = false)
    {
        Value lastValue = new Word("none"); // Default return value
        int index = 0;

        // Loop through every item in the block
        while (index < block.Children.Count)
        {
            lastValue = Next(block, ref index, context, isTail);

            // Update LastResult only if it's NOT a TailCall AND NOT 'none'
            // AND ONLY if we are NOT in a tail position ourselves (to avoid polluting it in recursion)
            if (!isTail && lastValue is not TailCall && (lastValue is not Word w || w.Name != "none"))
            {
                context.LastResult = lastValue;
            }

            if (lastValue is TailCall tc)
            {
                if (index < block.Children.Count)
                {
                    // This was NOT the last expression in the block, so we cannot return a TailCall.
                    // We must trampoline it here and continue with the next expression.
                    lastValue = ExecuteWithTrampoline(tc.Function, tc.Args, tc.Refinements, tc.Context);
                    if (!isTail && (lastValue is not Word w2 || w2.Name != "none"))
                    {
                        context.LastResult = lastValue;
                    }
                }
                else
                {
                    // This IS the last expression. Return the TailCall to the caller to handle.
                    return lastValue;
                }
            }
        }

        return lastValue;
    }

    // The 'Next' method evaluates the single next expression, including infix operators.
    public Value Next(Block block, ref int index, Context context, bool isTail = false)
    {
        // A prefix call is in tail position ONLY if there is no infix operator following it.
        Value left = NextExpression(block, ref index, context, isTail && !HasInfix(block, index, context));

        // Greedy infix lookahead (left-associative)
        while (index < block.Children.Count)
        {
            Value nextToken = block.Children[index];
            if (nextToken is Word w && context.TryGet(w.Name, out Value? v) && v is Op op)
            {
                index++; // consume the Op word
                // An infix call is in tail position ONLY if it's the very last thing in the block 
                // and there are no more infix operators following THIS one.
                bool lastInfix = !HasInfix(block, index, context);
                
                // Arguments to operators are NEVER in tail position.
                Value right = NextExpression(block, ref index, context, false);
                left = op.Action([left, right], [], context, this, isTail && lastInfix);
                
                // If the operator call itself was a tail call (unlikely for built-ins, but possible),
                // we should handle it.
                if (left is TailCall tc)
                {
                    if (index < block.Children.Count || !isTail)
                    {
                        left = ExecuteWithTrampoline(tc.Function, tc.Args, tc.Refinements, tc.Context);
                    }
                    else
                    {
                        return left; // Return the TailCall
                    }
                }
            }
            else
            {
                break;
            }
        }

        return left;
    }

    private bool HasInfix(Block block, int index, Context context)
    {
        if (index >= block.Children.Count) return false;
        Value nextToken = block.Children[index];
        return nextToken is Word w && context.TryGet(w.Name, out Value? v) && v is Op;
    }

    public Value NextExpression(Block block, ref int index, Context context, bool isTail = false)
    {
        if (index >= block.Children.Count)
            throw new Exception("Unexpected end of block: more arguments expected.");

        Value current = block.Children[index++];

        // --- PAREN EVALUATION ---
        if (current is Paren p)
        {
            return Evaluate(p, context, isTail);
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
            // Set-word result is never a tail call
            Value result = Next(block, ref index, context, false);
            context.Set(setWord.Name, result);
            return result;
        }

        // --- HANDLE REGULAR WORD ---
        if (current is Word word)
        {
            Value boundValue;
            if (word.Binding != null)
            {
                boundValue = word.Binding.Get(word.Name);
            }
            else
            {
                boundValue = context.Get(word.Name);
            }

            if (boundValue is Native native)
            {
                List<Value> args = [];
                for (int i = 0; i < native.Arity; i++)
                {
                    if (native.EvalArgs[i])
                    {
                        // Function arguments use Next to allow them to be greedy
                        // for infix operators (giving infix higher priority than prefix)
                        args.Add(Next(block, ref index, context, false));
                    }
                    else
                    {
                        if (index >= block.Children.Count) throw new Exception("Argument missing.");
                        args.Add(block.Children[index++]);
                    }
                }
                return native.Action(args, [], context, this, isTail);
            }

            if (boundValue is Function func)
            {
                var args = new List<Value>();
                foreach (var param in func.MainParameters)
                {
                    if (param.Evaluate)
                    {
                        args.Add(Next(block, ref index, context, false));
                    }
                    else
                    {
                        if (index >= block.Children.Count) throw new Exception("Argument missing.");
                        args.Add(block.Children[index++]);
                    }
                }
                
                if (isTail) return new TailCall(func, args, [], context);
                return ExecuteWithTrampoline(func, args, [], context);
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
            Value valueToSet = Next(block, ref index, context, false);
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
            else if (container is ObjectValue obj && lastSegment is Word w)
            {
                obj.Context.Set(w.Name, valueToSet);
                return valueToSet;
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
            bool isGetPath = path.Parts[0] is GetWord;
            Value currentVal = ResolvePathHead(context, path);
            for (int i = 1; i < path.Parts.Count; i++)
            {
                var segment = path.Parts[i];

                if (currentVal is Native or Function)
                {
                    if (isGetPath)
                    {
                        return currentVal;
                    }

                    var refinements = new List<string>();
                    for (int j = i; j < path.Parts.Count; j++)
                    {
                        if (path.Parts[j] is Word rw) refinements.Add(rw.Name);
                    }

                    if (currentVal is Native n)
                    {
                        var args = new List<Value>();
                        for (int k = 0; k < n.Arity; k++) args.Add(Next(block, ref index, context, false));
                        return n.Action(args, new HashSet<string>(refinements), context, this, isTail);
                    }
                    else if (currentVal is Function f)
                    {
                        var args = new List<Value>();
                        // Main args
                        foreach (var param in f.MainParameters)
                        {
                            if (param.Evaluate) args.Add(Next(block, ref index, context, false));
                            else
                            {
                                if (index >= block.Children.Count) throw new Exception("Argument missing.");
                                args.Add(block.Children[index++]);
                            }
                        }
                        
                        // Refinement args in order of refinements in the path
                        foreach (var refName in refinements)
                        {
                            var refSpec = f.Refinements.FirstOrDefault(r => r.Name == refName);
                            if (refSpec.Name != null)
                            {
                                foreach (var _ in refSpec.Args)
                                {
                                    args.Add(Next(block, ref index, context, false));
                                }
                            }
                        }

                        if (isTail) return new TailCall(f, args, new HashSet<string>(refinements), context);
                        return ExecuteWithTrampoline(f, args, refinements, context);
                    }
                }

                if (currentVal is ObjectValue obj && segment is Word key)
                {
                    currentVal = obj.Context.Get(key.Name);
                    if (currentVal is Function f)
                    {
                        // Auto-execute if it's a zero-argument function?
                        // Rebol: any word lookup that results in a function EXECUTES it.
                        if (!isGetPath && f.MainParameters.Count == 0)
                        {
                            currentVal = ExecuteWithTrampoline(f, [], [], obj.Context);
                        }
                    }
                    continue;
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

            // At the end of the path evaluation, if the final value is a Native or Function,
            // and it is NOT a get-path, execute it.
            if (!isGetPath && currentVal is Native or Function)
            {
                if (currentVal is Native n)
                {
                    var args = new List<Value>();
                    for (int k = 0; k < n.Arity; k++) args.Add(Next(block, ref index, context, false));
                    return n.Action(args, new HashSet<string>(), context, this, isTail);
                }
                else if (currentVal is Function f)
                {
                    var args = new List<Value>();
                    // Main args
                    foreach (var param in f.MainParameters)
                    {
                        if (param.Evaluate) args.Add(Next(block, ref index, context, false));
                        else
                        {
                            if (index >= block.Children.Count) throw new Exception("Argument missing.");
                            args.Add(block.Children[index++]);
                        }
                    }

                    if (isTail) return new TailCall(f, args, new HashSet<string>(), context);
                    return ExecuteWithTrampoline(f, args, [], context);
                }
            }

            return currentVal;
        }

        return current;
    }

    private Value NavigatePath(Value container, Value segment)
    {
        // 1. Handle Object property access
        if (container is ObjectValue obj && segment is Word key)
        {
            return obj.Context.Get(key.Name);
        }

        // 2. Handle .NET Instance or Static Type access
        if (container is DotNetValue dnv)
        {
            return GetDotNetMember(dnv.Instance, segment.ToString());
        }

        // 3. Handle Block index access (e.g., b/1)
        if (container is Block b && segment is Integer idx)
        {
            int listIdx = (int)idx.Number - 1; // Ragnar is 1-indexed
            return (listIdx >= 0 && listIdx < b.Children.Count)
                ? b.Children[listIdx]
                : new Word("none");
        }

        throw new Exception($"Cannot navigate into {container.GetType().Name} with segment {segment}");
    }

    private static Value ResolvePathHead(Context context, Path path)
    {
        var first = path.Parts[0];
        if (first is GetWord gw)
        {
            return context.Get(gw.Name);
        }
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
        var prop = Interop.GetPropertySafe(type, memberName, flags);
        if (prop != null) return Interop.ToRagnarValue(prop.GetValue(isStatic ? null : target));

        // Try Field
        var field = Interop.GetFieldSafe(type, memberName, flags);
        if (field != null) return Interop.ToRagnarValue(field.GetValue(isStatic ? null : target));

        // Try Method
        var methods = type.GetMethods(flags)
            .Where(m => string.Equals(m.Name, memberName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (methods.Count == 0 && !type.IsInterface)
        {
            foreach (var iface in type.GetInterfaces())
            {
                var ifaceMethods = iface.GetMethods(flags)
                    .Where(m => string.Equals(m.Name, memberName, StringComparison.OrdinalIgnoreCase));
                methods.AddRange(ifaceMethods);
            }
        }

        if (methods.Count > 0)
        {
            int minRequired = methods.Min(m => m.GetParameters().Count(p => !p.IsOptional));
            int arity = minRequired;
            var targetMethod = methods.OrderBy(m => m.GetParameters().Length).First();

            return new Native((args, refs, context, interpreter, isTail) =>
            {
                object?[] methodArgs = args.Select(Interop.ToNetObject).ToArray();
                Type[] argTypes = methodArgs.Select<object?, Type>(a => a?.GetType() ?? typeof(object)).ToArray();

                MethodInfo? bestMethod = null;
                bestMethod = type.GetMethod(memberName, flags, null, argTypes, null);
                if (bestMethod == null)
                {
                    foreach (var m in methods)
                    {
                        var parameters = m.GetParameters();
                        if (parameters.Length >= args.Count)
                        {
                            bool compatible = true;
                            for (int idx = 0; idx < args.Count; idx++)
                            {
                                var arg = methodArgs[idx];
                                var paramType = parameters[idx].ParameterType;
                                if (arg == null)
                                {
                                    if (paramType.IsValueType && Nullable.GetUnderlyingType(paramType) == null)
                                    {
                                        compatible = false;
                                        break;
                                    }
                                }
                                else if (!IsCompatible(arg, paramType))
                                {
                                    compatible = false;
                                    break;
                                }
                            }
                            if (compatible)
                            {
                                for (int idx = args.Count; idx < parameters.Length; idx++)
                                {
                                    if (!parameters[idx].IsOptional)
                                    {
                                        compatible = false;
                                        break;
                                    }
                                }
                            }
                            if (compatible)
                            {
                                bestMethod = m;
                                break;
                            }
                        }
                    }
                }

                if (bestMethod == null)
                {
                    bestMethod = methods.FirstOrDefault(m => m.GetParameters().Length == args.Count) ?? targetMethod;
                }

                var finalParams = bestMethod.GetParameters();
                object?[] finalArgs = new object?[finalParams.Length];
                for (int idx = 0; idx < finalParams.Length; idx++)
                {
                    if (idx < methodArgs.Length)
                    {
                        finalArgs[idx] = Interop.CoerceType(methodArgs[idx], finalParams[idx].ParameterType);
                    }
                    else
                    {
                        finalArgs[idx] = finalParams[idx].DefaultValue;
                    }
                }

                try
                {
                    object? result = bestMethod.Invoke(isStatic ? null : target, finalArgs);
                    return Interop.ToRagnarValue(result);
                }
                catch (TargetInvocationException ex)
                {
                    throw ex.InnerException ?? ex;
                }
            }, arity);
        }

        throw new Exception($"Member '{memberName}' not found on {type.Name}");
    }

    private Value ExecuteWithTrampoline(Function func, List<Value> args, IEnumerable<string> refinements, Context context)
    {
        Value result = ExecuteFunction(func, args, refinements, context);
        while (result is TailCall tc)
        {
            result = ExecuteFunction(tc.Function, tc.Args, tc.Refinements, tc.Context);
        }
        return result;
    }

    private Value ExecuteFunction(Function func, List<Value> args, IEnumerable<string> refinements, Context context)
    {
        // Hybrid scoping: Primary parent is the DefiningContext (Lexical).
        // Secondary parent is the caller context (Dynamic).
        var localContext = new Context(func.DefiningContext, context);
        localContext.IsFunctionFrame = true;
        int argIdx = 0;

        // Bind main parameters
        foreach (var param in func.MainParameters)
        {
            localContext.SetLocal(param.Name, argIdx < args.Count ? args[argIdx++] : new Word("none"));
        }

        // Bind refinements and their args
        var pathRefinements = refinements.ToList();
        var activeRefSet = new HashSet<string>(pathRefinements);

        // We need to associate refinement args in the order they appeared in the path
        var refArgMap = new Dictionary<string, List<Value>>();
        foreach (var refName in pathRefinements)
        {
            var refSpec = func.Refinements.FirstOrDefault(r => r.Name == refName);
            if (refSpec.Name != null)
            {
                var refArgs = new List<Value>();
                foreach (var _ in refSpec.Args)
                {
                    refArgs.Add(argIdx < args.Count ? args[argIdx++] : new Word("none"));
                }
                refArgMap[refName] = refArgs;
            }
        }

        foreach (var refSpec in func.Refinements)
        {
            bool isActive = activeRefSet.Contains(refSpec.Name);
            localContext.SetLocal(refSpec.Name, new Logic(isActive));
            
            for (int i = 0; i < refSpec.Args.Count; i++)
            {
                Value val = (isActive && refArgMap.ContainsKey(refSpec.Name)) 
                    ? refArgMap[refSpec.Name][i] 
                    : new Word("none");
                localContext.SetLocal(refSpec.Args[i], val);
            }
        }

        try
        {
            var result = Evaluate(func.Body, localContext, isTail: true);
            return result;
        }
        catch (ReturnException ex)
        {
            return ex.Value;
        }
    }

    private static bool IsCompatible(object? arg, Type targetType)
    {
        if (arg == null)
        {
            return !targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null;
        }
        if (targetType.IsAssignableFrom(arg.GetType())) return true;

        if (targetType.IsEnum && (arg is string || arg is Text || arg is Word || arg is LitWord)) return true;

        // Check if there is a static field/property of targetType with the given name
        string? staticName = null;
        if (arg is string str) staticName = str;
        else if (arg is Text txt) staticName = txt.Content;
        else if (arg is Word w) staticName = w.Name;
        else if (arg is LitWord lw) staticName = lw.Name;

        if (staticName != null)
        {
            var prop = targetType.GetProperty(staticName, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
            if (prop != null && targetType.IsAssignableFrom(prop.PropertyType)) return true;

            var field = targetType.GetField(staticName, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
            if (field != null && targetType.IsAssignableFrom(field.FieldType)) return true;
        }

        if (targetType == typeof(string) && (arg is Text || arg is Word || arg is LitWord)) return true;

        // Check if there is an implicit operator
        var implicitOp = targetType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Any(m => m.Name == "op_Implicit" && m.ReturnType == targetType && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType.IsAssignableFrom(arg.GetType()));
        if (implicitOp) return true;

        try
        {
            Convert.ChangeType(arg, targetType);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
