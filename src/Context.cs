
namespace Ragnar;

public class Context
{
    public TextWriter Output { get; set; } = Console.Out;
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
        _bindings[name] = value;
    }

    // Look up a value, searching up the parent chain if necessary
    public Value Get(string name)
    {
        if (_bindings.TryGetValue(name, out var value))
        {
            return value;
        }

        if (_parent != null)
        {
            return _parent.Get(name);
        }

        throw new Exception($"Word '{name}' has no value.");
    }

    // Check if a word exists in this context or parents
    public bool Exists(string name)
    {
        if (_bindings.ContainsKey(name)) return true;
        return _parent?.Exists(name) ?? false;
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
}