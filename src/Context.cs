
namespace rebelly;

public class Context(Context? parent = null)
{
    private readonly Dictionary<string, Value> _bindings = [];
    private readonly Context? _parent = parent;

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
}