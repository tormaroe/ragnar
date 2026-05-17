
namespace Ragnar;

public class Context
{
    public TextWriter Output { get; set; } = Console.Out;
    public Value? LastResult { get; set; }

    public Value? GetLastResult()
    {
        Context? current = this;
        while (current != null)
        {
            if (current.LastResult != null) return current.LastResult;
            current = current._parent;
        }
        return null;
    }
    private readonly Dictionary<string, Value> _bindings = [];
    private readonly Context? _parent;

    public Context(Context? parent = null)
    {
        _parent = parent;
        // Inherit the output stream from the parent if it exists
        if (parent != null) Output = parent.Output;
    }

    // Set a value in the CURRENT context
    public void Set(string name, Value value)
    {
        if (name == "it")
        {
            LastResult = value;
            return;
        }
        _bindings[name] = value;
    }

    // Look up a value, searching up the parent chain if necessary
    public Value Get(string name)
    {
        if (TryGet(name, out var value))
        {
            return value!;
        }

        throw new Exception($"Word '{name}' has no value.");
    }

    public bool TryGet(string name, out Value? value)
    {
        Context? current = this;
        while (current != null)
        {
            if (current._bindings.TryGetValue(name, out value))
            {
                return true;
            }
            current = current._parent;
        }

        if (name == "it")
        {
            value = GetLastResult();
            return value != null;
        }

        value = null;
        return false;
    }

    // Check if a word exists in this context or parents
    public bool Exists(string name)
    {
        if (_bindings.ContainsKey(name)) return true;
        if (_parent?.Exists(name) ?? false) return true;
        if (name == "it") return true;
        return false;
    }

    public Dictionary<string, Value> GetAllBindings()
    {
        // Start with the parent's bindings (if any)
        var all = _parent?.GetAllBindings() ?? [];

        // Overwrite/Add with current context bindings
        foreach (var kvp in _bindings)
        {
            all[kvp.Key] = kvp.Value;
        }

        return all;
    }

    public Dictionary<string, Value> GetOwnBindings()
    {
        return new Dictionary<string, Value>(_bindings);
    }
}