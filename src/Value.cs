using System.Globalization;
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
    public override string ToString() => Number.ToString(CultureInfo.InvariantCulture);
}

public abstract class Series : Value
{
    public int Index { get; set; } = 0;
    public abstract int Length { get; }
    public abstract Series At(int newIndex);
}

public class StringHolder(string value)
{
    public string Value { get; set; } = value;
}

public class Text : Series
{
    public StringHolder Holder { get; }
    public string Content
    {
        get => Holder.Value;
        set => Holder.Value = value;
    }
    public override int Length => Math.Max(0, Content.Length - Index);
    
    public Text(string value, int index = 0) 
    { 
        Holder = new StringHolder(value); 
        Index = index; 
    }

    public Text(StringHolder holder, int index = 0)
    {
        Holder = holder;
        Index = index;
    }

    public override string ToString() => $"\"{Content[Index..].Replace("\"", "\\\"")}\"";
    public override string ToUserString() => Content[Index..];
    public override Series At(int newIndex) => new Text(Holder, newIndex);
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
    public List<Value> Children { get; }
    public override int Length => Math.Max(0, Children.Count - Index);

    public Block(int index = 0) 
    { 
        Children = []; 
        Index = index; 
    }
    public Block(List<Value> children, int index)
    {
        Children = children;
        Index = index;
    }
    public Block(IEnumerable<Value> values, int index = 0)
    {
        Children = new List<Value>(values);
        Index = index;
    }

    public override string ToString() => "[ " + string.Join(" ", Children.Skip(Index)) + " ]";

    // Flatten the block: no brackets, and use the user-friendly version of children
    public override string ToUserString()
    {
        var sb = new StringBuilder();
        foreach (var child in Children.Skip(Index))
        {
            sb.Append(child.ToUserString());
        }
        return sb.ToString();
    }

    public override Series At(int newIndex) => new Block(Children, newIndex);
}

public class Paren : Block
{
    public Paren(int index = 0) : base(index) { }
    public Paren(List<Value> children, int index) : base(children, index) { }
    public Paren(IEnumerable<Value> parts, int index = 0) : base(parts, index) { }

    public override string ToString() => "(" + string.Join(" ", Children.Skip(Index)) + ")";
    public override Series At(int newIndex) => new Paren(Children, newIndex);
}

public class Record : Block
{
    public Record(int index = 0) : base(index) { }
    public Record(List<Value> children, int index) : base(children, index) { }
    public Record(IEnumerable<Value> values, int index = 0) : base(values, index) { }

    public override string ToString() => "#( " + string.Join(" ", Children.Skip(Index)) + " )";
    public override Series At(int newIndex) => new Record(Children, newIndex);
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
    public List<string> Refinements { get; } = new();

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

    public Native WithRefinements(params string[] refinements)
    {
        Refinements.AddRange(refinements);
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
    public List<(string Name, bool Evaluate)> MainParameters { get; }
    public List<(string Name, List<string> Args)> Refinements { get; }
    public Block Body { get; }
    public string Title { get; set; } = "";
    public Context? DefiningContext { get; }

    public Function(List<(string Name, bool Evaluate)> mainParameters, List<(string Name, List<string> Args)> refinements, Block body, string title = "", Context? definingContext = null)
    {
        MainParameters = mainParameters;
        Refinements = refinements;
        Body = body;
        Title = title;
        DefiningContext = definingContext;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append("func [ ");
        if (!string.IsNullOrEmpty(Title))
        {
            sb.Append($"\"{Title.Replace("\"", "\\\"")}\" ");
        }

        foreach (var p in MainParameters)
        {
            if (!p.Evaluate) sb.Append("'");
            sb.Append(p.Name + " ");
        }
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
    public override string ToUserString() => Path;
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

public class Character(char value) : Value
{
    public char CharValue { get; } = value;
    public override string ToString()
    {
        if (CharValue == '\n') return "#\"^/\"";
        if (CharValue == '\t') return "#\"^-\"";
        if (CharValue == '^') return "#\"^^\"";
        if (CharValue == '"') return "#\"^\"\"";
        return $"#\"{CharValue}\"";
    }
    public override string ToUserString() => CharValue.ToString();
}

/// <summary>
/// A set of characters, produced by <c>charset</c>. Used in parse rules to match
/// any single character that is a member of the set.
/// </summary>
public class Bitset(HashSet<char> chars) : Value
{
    public HashSet<char> Chars { get; } = chars;

    public bool Contains(char c) => Chars.Contains(c);

    public override string ToString() => $"make bitset! [{string.Join(" ", Chars.Select(c => $"#{c}"))}]";
}

public class ErrorValue(string message, Exception? innerException = null) : Value
{
    public string Message { get; } = message;
    public Exception? InnerException { get; } = innerException;

    public override string ToString() => $"** Script Error: {Message}";
}

public class GuiWidget : Value
{
    public string Id { get; }
    public string Type { get; }
    public string Text { get; set; }
    public Value CurrentValue { get; set; }
    public Block? Action { get; }
    public List<GuiWidget> Children { get; } = new();
    public List<string> Options { get; } = new();
    public string? Width { get; set; }
    public string? Height { get; set; }

    public GuiWidget(string id, string type, string text, Value currentValue, Block? action = null, List<string>? options = null)
    {
        Id = id;
        Type = type;
        Text = text;
        CurrentValue = currentValue;
        Action = action;
        if (options != null) Options.AddRange(options);
    }

    public override string ToString() => $"<gui-widget {Type} id:{Id}>";
}