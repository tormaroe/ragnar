namespace rebelly;

public class Interop
{
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

    public static void AddInteropFunctions(Context ctx)
    {
        // get-type "System.Text.StringBuilder"
        ctx.Set("get-type", new Native((args, _, _) => {
            string typeName = (args[0] as Text)?.Content 
                ?? throw new Exception("get-type requires a string.");
            
            Type? type = Type.GetType(typeName);
            if (type == null) throw new Exception($"Could not find .NET type: {typeName}");
            
            return new DotNetValue(type);
        }, 1));
        
        ctx.Set("new", new Native((args, context, interpreter) => {
            if (args[0] is DotNetValue dnv && dnv.Instance is Type type)
            {
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

                // Now convert those evaluated Rebelly values to .NET objects
                object?[] constructorArgs = evaluatedArgs
                    .Select(ToNetObject)
                    .ToArray();

                try 
                {
                    return new DotNetValue(Activator.CreateInstance(type, constructorArgs));
                }
                catch (Exception ex)
                {
                    var msg = ex.InnerException?.Message ?? ex.Message;
                    throw new Exception($"Failed to instantiate {type.Name}: {msg}");
                }
            }
            throw new Exception("'new' requires a .NET Type.");
        }, 2));
        
        ctx.Set("call-method", new Native((args, context, interpreter) => {
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
                
                return result switch {
                    null => new Word("none"),
                    int i => new Integer(i),
                    long l => new Integer(l),
                    double d => new Decimal(d),
                    float f => new Decimal(f),
                    string s => new Text(s),
                    bool b => new Logic(b),
                    _ => new DotNetValue(result)
                };
            }
            throw new Exception("call-method usage: obj \"name\" [args]");
        }, 3));

        // get-prop obj "Length"
        ctx.Set("get-prop", new Native((args, _, _) => {
            if (args[0] is DotNetValue dnv && args[1] is Text propName)
            {
                var prop = dnv.Instance?.GetType().GetProperty(propName.Content);
                if (prop == null) throw new Exception($"Property '{propName.Content}' not found.");
                
                object? val = prop.GetValue(dnv.Instance);
                
                // Convert back to Rebelly types if possible
                return val switch {
                    int i => new Integer(i),
                    long l => new Integer(l),
                    string s => new Text(s),
                    _ => new DotNetValue(val)
                };
            }
            throw new Exception("get-prop requires an object and a property name.");
        }, 2));

        // set-prop obj "PropertyName" value
        ctx.Set("set-prop", new Native((args, context, interpreter) => {
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
        }, 3));
    }
}