using System.Reflection;

namespace Ragnar;

public class Interop
{
    public static Type ResolveType(string name)
    {
        // 1. Try the standard way (only checks core/calling assembly)
        var type = Type.GetType(name);
        if (type != null) return type;

        // 2. Search all assemblies currently loaded in the forge
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(name);
            if (type != null) return type;
        }

        throw new Exception($"Type not found: {name}");
    }

    public static object? ToNetObject(Value value)
    {
        switch (value)
        {
            case Integer i:
                // Explicitly return as a boxed object to prevent auto-promotion to long
                if (i.Number >= int.MinValue && i.Number <= int.MaxValue)
                    return (int)i.Number;
                return i.Number;

            case Decimal d:
                if (d.Number >= float.MinValue && d.Number <= float.MaxValue)
                    return (float)d.Number;
                return d.Number;

            case Text t:
                return t.Content;

            case Logic l:
                return l.Condition;

            case DotNetValue dnv:
                return dnv.Instance;

            default:
                return value;
        }
    }

    public static Value ToRagnarValue(object? obj)
    {
        if (obj == null) return new Word("none");

        return obj switch
        {
            int i => new Integer(i),
            long l => new Integer(l),
            double d => new Decimal(d),
            float f => new Decimal(f),
            string s => new Text(s),
            bool b => new Logic(b),
            Value v => v, // Already a Ragnar value
            _ => new DotNetValue(obj) // Keep complex objects wrapped
        };
    }

    public static void SetDotNetMember(object? target, string memberName, Value ragnarValue)
    {
        if (target == null) throw new Exception("Cannot set member on null object.");

        bool isStatic = target is Type;
        Type type = isStatic ? (Type)target : target.GetType();
        var flags = BindingFlags.Public | BindingFlags.IgnoreCase |
                    (isStatic ? BindingFlags.Static : BindingFlags.Instance);

        object? rawValue = ToNetObject(ragnarValue);

        // Try Property first
        var prop = type.GetProperty(memberName, flags);
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(isStatic ? null : target, rawValue);
            return;
        }

        // Try Field
        var field = type.GetField(memberName, flags);
        if (field != null)
        {
            field.SetValue(isStatic ? null : target, rawValue);
            return;
        }

        throw new Exception($"Member '{memberName}' not found or not writable on {type.Name}");
    }

    public static void AddInteropFunctions(Context ctx)
    {
        // get-type "System.Text.StringBuilder"
        ctx.Set("get-type", new Native((args, refinements, _, _, isTail) =>
        {
            string typeName = (args[0] as Text)?.Content
                ?? throw new Exception("get-type requires a string.");

            Type? type = Type.GetType(typeName);
            if (type == null) throw new Exception($"Could not find .NET type: {typeName}");

            return new DotNetValue(type);
        }, 1).WithTitle("Returns the .NET Type for a given name."));

        ctx.Set("new", new Native((args, refinements, context, interpreter, isTail) =>
        {
                Type? targetType = null;

                // 1. Resolve the Type from the first argument
                if (args[0] is Text t)
                {
                    // Try to find the type by name
                    targetType = Type.GetType(t.Content) ??
                                 AppDomain.CurrentDomain.GetAssemblies()
                                    .Select(a => a.GetType(t.Content))
                                    .FirstOrDefault(x => x != null);
                }
                else if (args[0] is DotNetValue dnv && dnv.Instance is Type typeFromPath)
                {
                    targetType = typeFromPath;
                }

                if (targetType == null)
                    throw new Exception($"Could not resolve .NET type: {args[0]}");

                if (args[1] is not Block argBlock)
                    throw new Exception("'new' requires a block of arguments.");

                // --- FIX: Evaluate the contents of the block first ---
                var evaluatedArgs = new List<Value>();
                foreach (var child in argBlock.Children)
                {
                    // We evaluate each child individually. 
                    // If it's a Word or GetWord, it gets resolved.
                    // If it's a literal, it stays a literal.
                    var tempBlock = new Block(new[] { child });
                    evaluatedArgs.Add(interpreter.Evaluate(tempBlock, context));
                }

                // Now convert those evaluated Ragnar values to .NET objects
                object?[] constructorArgs = evaluatedArgs
                    .Select(ToNetObject)
                    .ToArray();

                try
                {
                    return new DotNetValue(Activator.CreateInstance(targetType, constructorArgs));
                }
                catch (Exception ex)
                {
                    var msg = ex.InnerException?.Message ?? ex.Message;
                    throw new Exception($"Failed to instantiate {targetType.Name}: {msg}");
                }
        }, 2).WithTitle("Creates a new .NET object instance."));

        ctx.Set("call-method", new Native((args, refinements, context, interpreter, isTail) =>
        {
            if (args[0] is DotNetValue dnv && args[1] is Text methodName && args[2] is Block argBlock)
            {
                // 1. Evaluate every argument in the block
                var evaluatedArgs = new List<Value>();
                foreach (var child in argBlock.Children)
                {
                    var tempBlock = new Block(new[] { child });
                    evaluatedArgs.Add(interpreter.Evaluate(tempBlock, context));
                }

                // 2. Unbox to .NET objects
                object?[] methodArgs = evaluatedArgs.Select(ToNetObject).ToArray();
                // We explicitly tell the Select method to return Type (not Type?)
                Type[] argTypes = methodArgs.Select<object?, Type>(a => a?.GetType() ?? typeof(object)).ToArray();

                // 3. Find the method that matches the signature
                var method = dnv.Instance?.GetType().GetMethod(methodName.Content, argTypes);

                if (method == null)
                    throw new Exception($"Method '{methodName.Content}' not found for the provided argument types.");

                // 4. Invoke and wrap the result
                object? result = method.Invoke(dnv.Instance, methodArgs);

                return ToRagnarValue(result);
            }
            throw new Exception("call-method usage: obj \"name\" [args]");
        }, 3).WithTitle("Calls a method on a .NET object."));

        // get-prop obj "Length"
        ctx.Set("get-prop", new Native((args, refinements, _, _, isTail) =>
        {
            if (args[0] is DotNetValue dnv && args[1] is Text propName)
            {
                var prop = dnv.Instance?.GetType().GetProperty(propName.Content);
                if (prop == null) throw new Exception($"Property '{propName.Content}' not found.");

                object? val = prop.GetValue(dnv.Instance);
                return ToRagnarValue(val);
            }
            throw new Exception("get-prop requires an object and a property name.");
        }, 2).WithTitle("Returns the value of a .NET property."));

        // set-prop obj "PropertyName" value
        ctx.Set("set-prop", new Native((args, refinements, context, interpreter, isTail) =>
        {
            if (args[0] is DotNetValue dnv && args[1] is Text propName)
            {
                var instance = dnv.Instance;
                if (instance == null) throw new Exception("Target object is null.");

                var prop = instance.GetType().GetProperty(propName.Content);
                if (prop == null) throw new Exception($"Property '{propName.Content}' not found on {instance.GetType().Name}.");

                // The third argument (args[2]) is already evaluated by the interpreter
                object? netValue = ToNetObject(args[2]);

                try
                {
                    // Reflection can be picky. If the property expects a specific type 
                    // that ToNetObject didn't catch (like an Enum or a byte), 
                    // Convert.ChangeType can provide an extra layer of safety.
                    object? finalValue = netValue;
                    if (netValue != null && prop.PropertyType != netValue.GetType())
                    {
                        finalValue = Convert.ChangeType(netValue, prop.PropertyType);
                    }

                    prop.SetValue(instance, finalValue);
                    return args[2]; // Return the value that was set (Rebol convention)
                }
                catch (Exception ex)
                {
                    var msg = ex.InnerException?.Message ?? ex.Message;
                    throw new Exception($"Failed to set property '{propName.Content}': {msg}");
                }
            }
            throw new Exception("set-prop usage: obj \"name\" value");
        }, 3).WithTitle("Sets the value of a .NET property."));

        // get-static "System.DateTime" "Now"
        ctx.Set("get-static", new Native((args, refs, context, interpreter, isTail) =>
        {
            string typeName = (args[0] as Text)?.Content ?? "";
            string memberName = (args[1] as Text)?.Content ?? "";
            Type type = ResolveType(typeName);

            // Check for Property
            var prop = type.GetProperty(memberName, BindingFlags.Static | BindingFlags.Public);
            if (prop != null) return ToRagnarValue(prop.GetValue(null));

            // Check for Field
            var field = type.GetField(memberName, BindingFlags.Static | BindingFlags.Public);
            if (field != null) return ToRagnarValue(field.GetValue(null));

            throw new Exception($"Static member {memberName} not found on {typeName}");
        }, 2).WithTitle("Returns the value of a static .NET member."));

        // call-static "System.Math" "Sqrt" [25.0]
        ctx.Set("call-static", new Native((args, refs, context, interpreter, isTail) =>
        {
            string typeName = (args[0] as Text)?.Content ?? throw new Exception("Type name required.");
            string methodName = (args[1] as Text)?.Content ?? throw new Exception("Method name required.");
            Block methodArgs = args[2] as Block ?? throw new Exception("Args must be a block.");

            Type type = ResolveType(typeName);

            // 1. Convert Ragnar args to .NET objects and get their types
            object?[] invokeArgs = methodArgs.Children.Select(ToNetObject).ToArray();
            Type[] argTypes = invokeArgs.Select(a => a?.GetType() ?? typeof(object)).ToArray();

            // 2. Find the specific method that matches these types
            var method = type.GetMethod(methodName,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy,
                null, argTypes, null);

            if (method == null)
                throw new Exception($"No static method {methodName} on {typeName} matches these arguments.");

            // 3. Execute and convert the result back to Ragnar
            return ToRagnarValue(method.Invoke(null, invokeArgs));
        }, 3).WithTitle("Calls a static .NET method."));
    }
}