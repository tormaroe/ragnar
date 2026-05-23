
namespace Ragnar;

public class Context
{
    public TextWriter Output { get; set; } = Console.Out;
    public Value? LastResult { get; set; }

    private readonly Dictionary<string, Value> _bindings = [];
    private readonly Context? _parent;
    private readonly Context? _secondaryParent;
    private ActorInstance? _actor;

    public ActorInstance Actor => _actor ??= new ActorInstance();

    public Context(Context? parent = null, Context? secondaryParent = null)
    {
        _parent = parent;
        _secondaryParent = secondaryParent;
        // Inherit the output stream from the parent if it exists
        if (parent != null) Output = parent.Output;
        else if (secondaryParent != null) Output = secondaryParent.Output;
    }

    // Set a value, searching up the parent chain to update an existing binding
    // if one exists. Otherwise, set it in the current context.
    public void Set(string name, Value value)
    {
        if (name == "it")
        {
            LastResult = value;
            return;
        }

        if (TryUpdate(name, value)) return;

        // Not found in any context, so set it in the current one.
        _bindings[name] = value;
    }

    // Set a value strictly in the current context, ignoring parent bindings.
    public void SetLocal(string name, Value value)
    {
        if (name == "it")
        {
            LastResult = value;
            return;
        }
        _bindings[name] = value;
    }

    private bool TryUpdate(string name, Value value)
    {
        Context? current = this;
        while (current != null)
        {
            if (current._bindings.ContainsKey(name))
            {
                current._bindings[name] = value;
                return true;
            }
            current = current._parent;
        }

        if (_secondaryParent != null && _secondaryParent.TryUpdate(name, value))
        {
            return true;
        }

        return false;
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

        if (_secondaryParent != null && _secondaryParent.TryGet(name, out value))
        {
            return true;
        }

        if (name == "it")
        {
            value = GetLastResult();
            return value != null;
        }

        if (name == "self")
        {
            value = new DotNetValue(Actor);
            return true;
        }

        value = null;
        return false;
    }

    public Value? GetLastResult()
    {
        Context? current = this;
        while (current != null)
        {
            if (current.LastResult != null) return current.LastResult;
            current = current._parent;
        }

        if (_secondaryParent != null) return _secondaryParent.GetLastResult();

        return null;
    }

    // Check if a word exists in this context or parents
    public bool Exists(string name)
    {
        if (_bindings.ContainsKey(name)) return true;
        if (_parent?.Exists(name) ?? false) return true;
        if (_secondaryParent?.Exists(name) ?? false) return true;
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