namespace Ragnar;

public abstract class Value
{
    public abstract override string ToString();
    // Default human-friendly version is just the string version
    public virtual string ToUserString() => ToString();
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
    public override string ToString() => $"\"{Content}\"";
    public override string ToUserString() => Content;
}

public class Word(string name) : Value
{
    public string Name { get; } = name;
    public override string ToString() => Name;
}

public class LitWord(string name) : Value
{
    public string Name { get; } = name.TrimStart('\'');
    public override string ToString() => "'" + Name;
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

    public override string ToString() => "[ " + string.Join(" ", Children) + " ]";

    // Flatten the block: no brackets, and use the user-friendly version of children
    public override string ToUserString() => 
        string.Join(" ", Children.Select(c => c.ToUserString()));
}

// The new signature includes the refinements HashSet
public delegate Value NativeDelegate(
    List<Value> args, 
    HashSet<string> refinements, 
    Context context, 
    Interpreter interpreter
);

public class Native(NativeDelegate action, int arity) : Value
{
    public NativeDelegate Action { get; } = action;
    public int Arity { get; } = arity;

    public override string ToString() => $"<native arity:{Arity}>";
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

public class File(string path) : Value
{
    public string Path { get; } = path.TrimStart('%');
    public override string ToString() => "%" + Path;
}

public class Refinement(string name) : Value
{
    public string Name { get; } = name.TrimStart('/');
    public override string ToString() => "/" + Name;
}

public class Path(IEnumerable<Value> parts) : Value
{
    public List<Value> Parts { get; } = parts.ToList();
    // Reconstruct with slashes: a/b/c
    public override string ToString() => string.Join("/", Parts.Select(p => p.ToString()));
}

public class SetPath(IEnumerable<Value> parts) : Path(parts)
{
    public override string ToString() => string.Join("/", Parts.Select(p => p.ToString())) + ":";
}

public class DotNetValue(object? instance) : Value
{
    public object? Instance { get; } = instance;
    public override string ToString() => Instance?.ToString() ?? "null";
}