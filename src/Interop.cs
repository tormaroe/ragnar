using System.Reflection;

namespace Ragnar;

public class Interop
{
    static Interop()
    {
        try
        {
            _ = typeof(Microsoft.Data.SqlClient.SqlConnection).FullName;
            _ = typeof(ClosedXML.Excel.XLWorkbook).FullName;
        }
        catch {}
    }

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

            case Word w when w.Name == "none":
                return null;

            case Block b:
                return b.Children.Skip(b.Index).Select(ToNetObject).ToList();

            default:
                return value;
        }
    }

    public static Value ToRagnarValue(object? obj)
    {
        if (obj == null || obj == DBNull.Value) return new Word("none");

        return obj switch
        {
            byte b => new Integer(b),
            short s => new Integer(s),
            int i => new Integer(i),
            long l => new Integer(l),
            double d => new Decimal(d),
            float f => new Decimal(f),
            decimal dec => new Decimal((double)dec),
            char c => new Character(c),
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
        var prop = GetPropertySafe(type, memberName, flags);
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(isStatic ? null : target, CoerceType(rawValue, prop.PropertyType));
            return;
        }

        // Try Field
        var field = GetFieldSafe(type, memberName, flags);
        if (field != null)
        {
            field.SetValue(isStatic ? null : target, CoerceType(rawValue, field.FieldType));
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
                int blockIdx = 0;
                while (blockIdx < argBlock.Children.Count)
                {
                    evaluatedArgs.Add(interpreter.Next(argBlock, ref blockIdx, context));
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
                int blockIdx = 0;
                while (blockIdx < argBlock.Children.Count)
                {
                    evaluatedArgs.Add(interpreter.Next(argBlock, ref blockIdx, context));
                }

                // 2. Unbox to .NET objects
                object?[] methodArgs = evaluatedArgs.Select(ToNetObject).ToArray();
                // We explicitly tell the Select method to return Type (not Type?)
                Type[] argTypes = methodArgs.Select<object?, Type>(a => a?.GetType() ?? typeof(object)).ToArray();

                // 3. Find the method that matches the signature
                var method = dnv.Instance?.GetType().GetMethod(methodName.Content, argTypes);

                if (method == null && dnv.Instance != null)
                {
                    foreach (var iface in dnv.Instance.GetType().GetInterfaces())
                    {
                        method = iface.GetMethod(methodName.Content, argTypes);
                        if (method != null) break;
                    }
                }

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
            if (args[0] is DotNetValue dnv && args[1] is Text propName && dnv.Instance != null)
            {
                var flags = BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static;
                var prop = GetPropertySafe(dnv.Instance.GetType(), propName.Content, flags);
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

                var flags = BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Instance;
                var prop = GetPropertySafe(instance.GetType(), propName.Content, flags);
                if (prop == null) throw new Exception($"Property '{propName.Content}' not found on {instance.GetType().Name}.");

                // The third argument (args[2]) is already evaluated by the interpreter
                object? netValue = ToNetObject(args[2]);

                try
                {
                    object? finalValue = CoerceType(netValue, prop.PropertyType);

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

    public static object? CoerceType(object? arg, Type targetType)
    {
        if (arg == null) return null;
        if (targetType.IsAssignableFrom(arg.GetType())) return arg;

        // Check for Enum conversion
        if (targetType.IsEnum)
        {
            string? s = null;
            if (arg is string str) s = str;
            else if (arg is Text txt) s = txt.Content;
            else if (arg is Word w) s = w.Name;
            else if (arg is LitWord lw) s = lw.Name;

            if (s != null)
            {
                try
                {
                    return Enum.Parse(targetType, s, true);
                }
                catch {}
            }
        }
        else
        {
            // Check for static field/property of targetType matching the name
            string? s = null;
            if (arg is string str) s = str;
            else if (arg is Text txt) s = txt.Content;
            else if (arg is Word w) s = w.Name;
            else if (arg is LitWord lw) s = lw.Name;

            if (s != null)
            {
                var prop = targetType.GetProperty(s, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
                if (prop != null && targetType.IsAssignableFrom(prop.PropertyType))
                {
                    try { return prop.GetValue(null); } catch {}
                }

                var field = targetType.GetField(s, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
                if (field != null && targetType.IsAssignableFrom(field.FieldType))
                {
                    try { return field.GetValue(null); } catch {}
                }
            }
        }

        // Check for String conversion from Word/LitWord
        if (targetType == typeof(string))
        {
            if (arg is string str) return str;
            if (arg is Text txt) return txt.Content;
            if (arg is Word w) return w.Name;
            if (arg is LitWord lw) return lw.Name;
        }

        // Check for implicit conversion operator on the target type
        var implicitOp = targetType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == "op_Implicit" && m.ReturnType == targetType && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType.IsAssignableFrom(arg.GetType()));
        
        if (implicitOp != null)
        {
            try
            {
                return implicitOp.Invoke(null, new[] { arg });
            }
            catch {}
        }

        // Standard ChangeType fallback
        try
        {
            return Convert.ChangeType(arg, targetType);
        }
        catch
        {
            return arg;
        }
    }

    public static PropertyInfo? GetPropertySafe(Type type, string name, BindingFlags flags)
    {
        PropertyInfo? prop = null;
        try
        {
            prop = type.GetProperty(name, flags);
        }
        catch (AmbiguousMatchException)
        {
            var props = type.GetProperties(flags)
                .Where(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (props.Count > 0)
                prop = props.OrderBy(p => GetInheritanceDistance(type, p.DeclaringType)).First();
        }

        if (prop == null && !type.IsInterface)
        {
            foreach (var iface in type.GetInterfaces())
            {
                prop = GetPropertySafe(iface, name, flags);
                if (prop != null) break;
            }
        }

        return prop;
    }

    public static FieldInfo? GetFieldSafe(Type type, string name, BindingFlags flags)
    {
        try
        {
            return type.GetField(name, flags);
        }
        catch (AmbiguousMatchException)
        {
            var fields = type.GetFields(flags)
                .Where(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (fields.Count == 0) return null;
            return fields.OrderBy(f => GetInheritanceDistance(type, f.DeclaringType)).First();
        }
    }

    private static int GetInheritanceDistance(Type derived, Type? declaring)
    {
        if (declaring == null) return int.MaxValue;
        int distance = 0;
        var current = derived;
        while (current != null && current != declaring)
        {
            distance++;
            current = current.BaseType;
        }
        return current == declaring ? distance : int.MaxValue;
    }
}