namespace rebelly;

public abstract class Value
{
    // Every value should be able to represent itself as a string
    public abstract override string ToString();
}

public class Integer(long value) : Value
{
    public long Number { get; } = value;
    public override string ToString() => Number.ToString();
}

public class Decimal(double value) : Value
{
    public double Number { get; } = value;
    public override string ToString() => Number.ToString();
}

public class Text(string value) : Value
{
    public string Content { get; } = value;
    public override string ToString() => Content;
}

public class Word(string name) : Value
{
    public string Name { get; } = name;
    public override string ToString() => Name;
}

public class SetWord(string name) : Value
{
    // We strip the colon for the Name so it matches the Word lookup later
    public string Name { get; } = name.TrimEnd(':');
    public override string ToString() => Name + ":";
}

public class GetWord(string name) : Value
{
    // Strip the leading colon
    public string Name { get; } = name.TrimStart(':');
    public override string ToString() => ":" + Name;
}

public class Block : Value
{
    public List<Value> Children { get; } = [];

    public Block() { }
    public Block(IEnumerable<Value> values) => Children.AddRange(values);

    public override string ToString() 
    {
        return $"[ {string.Join(" ", Children.Select(c => c.ToString()))} ]";
    }
}

public delegate Value NativeAction(List<Value> args, Context context, Interpreter interpreter);

public class Native(NativeAction action, int arity) : Value
{
    public NativeAction Action { get; } = action;
    public int Arity { get; } = arity;
    public override string ToString() => "<native-function>";
}

public class Logic(bool value) : Value
{
    public bool Condition { get; } = value;
    public override string ToString() => Condition ? "true" : "false";
}

public class Function(List<string> parameters, Block body) : Value
{
    public List<string> Parameters { get; } = parameters;
    public Block Body { get; } = body;

    public override string ToString() => "<user-function>";
}

public class DotNetValue(object? instance) : Value
{
    public object? Instance { get; } = instance;
    public override string ToString() => Instance?.ToString() ?? "null";
}