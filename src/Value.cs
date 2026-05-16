using System.Text;

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

public abstract class Series : Value
{
    public int Index { get; set; } = 0;
    public abstract int Length { get; }
    public abstract Series At(int newIndex);
}

public class Text : Series
{
    public string Content { get; set; }
    public override int Length => Math.Max(0, Content.Length - Index);
    
    public Text(string value, int index = 0) 
    { 
        Content = value; 
        Index = index; 
    }

    public override string ToString() => $"\"{Content[Index..].Replace("\"", "\\\"")}\"";
    public override string ToUserString() => Content[Index..];
    public override Series At(int newIndex) => new Text(Content, newIndex);
}

public class Word(string name, Context? binding = null) : Value
{
    public string Name { get; } = name;
    public Context? Binding { get; set; } = binding;
    public override string ToString() => Name;
}

public class ObjectValue(Context context) : Value
{
    public Context Context { get; } = context;
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append("make object! [ ");
        foreach (var kvp in Context.GetOwnBindings())
        {
            if (kvp.Key == "self") continue;
            sb.Append(kvp.Key);
            sb.Append(": ");
            sb.Append(kvp.Value.ToString());
            sb.Append(" ");
        }
        sb.Append("]");
        return sb.ToString();
    }
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

public class Block : Series
{
    public List<Value> Children { get; } = [];
    public override int Length => Math.Max(0, Children.Count - Index);

    public Block(int index = 0) { Index = index; }
    public Block(IEnumerable<Value> values, int index = 0)
    {
        Children.AddRange(values);
        Index = index;
    }

    public override string ToString() => "[ " + string.Join(" ", Children.Skip(Index)) + " ]";

    // Flatten the block: no brackets, and use the user-friendly version of children
    public override string ToUserString() => 
        string.Join(" ", Children.Skip(Index).Select(c => c.ToUserString()));

    public override Series At(int newIndex) => new Block(Children, newIndex);
}

public class Paren : Block
{
    public Paren(int index = 0) : base(index) { }
    public Paren(IEnumerable<Value> parts, int index = 0) : base(parts, index) { }

    public override string ToString() => "(" + string.Join(" ", Children.Skip(Index)) + ")";
    public override Series At(int newIndex) => new Paren(Children, newIndex);
}

// The new signature includes the refinements HashSet and isTail flag
public delegate Value NativeDelegate(
    List<Value> args, 
    HashSet<string> refinements, 
    Context context, 
    Interpreter interpreter,
    bool isTail
);

public class Native : Value
{
    public NativeDelegate Action { get; }
    public int Arity { get; }
    // Define which arguments should be evaluated. 
    // True = Evaluate (default), False = Literal (Word remains a Word)
    public bool[] EvalArgs { get; }
    public string Title { get; set; } = "";

    public Native(NativeDelegate action, int arity, bool[]? evalArgs = null)
    {
        Action = action;
        Arity = arity;
        // If no spec is provided, we default to evaluating everything.
        EvalArgs = evalArgs ?? Enumerable.Repeat(true, arity).ToArray();
    }

    public Native WithTitle(string title)
    {
        Title = title;
        return this;
    }

    public override string ToString() => $"<native arity:{Arity}>";
}

public class Op(NativeDelegate action) : Native(action, 2)
{
    public override string ToString() => "<op>";
}

public class Logic(bool value) : Value
{
    public bool Condition { get; } = value;
    public override string ToString() => Condition ? "true" : "false";
}

public class Function : Value
{
    public List<string> MainParameters { get; }
    public List<(string Name, List<string> Args)> Refinements { get; }
    public Block Body { get; }
    public string Title { get; set; } = "";

    public Function(List<string> mainParameters, List<(string Name, List<string> Args)> refinements, Block body, string title = "")
    {
        MainParameters = mainParameters;
        Refinements = refinements;
        Body = body;
        Title = title;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append("func [ ");
        if (!string.IsNullOrEmpty(Title))
        {
            sb.Append($"\"{Title.Replace("\"", "\\\"")}\" ");
        }

        foreach (var p in MainParameters) sb.Append(p + " ");
        foreach (var r in Refinements)
        {
            sb.Append("/" + r.Name + " ");
            foreach (var arg in r.Args) sb.Append(arg + " ");
        }
        sb.Append("] ");
        sb.Append(Body.ToString());
        return sb.ToString();
    }
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

public class TailCall : Value
{
    public Function Function { get; }
    public List<Value> Args { get; }
    public HashSet<string> Refinements { get; }
    public Context Context { get; }

    public TailCall(Function function, List<Value> args, HashSet<string> refinements, Context context)
    {
        Function = function;
        Args = args;
        Refinements = refinements;
        Context = context;
    }

    public override string ToString() => "<tail-call>";
}