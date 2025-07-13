using Silk.NET.Input;

namespace Engine.Input.Contexts;

public class InputContext
{
    private readonly Dictionary<long, InputContextEntry> _entries;

    private InputContext(Dictionary<long, InputContextEntry> entries)
    {
        _entries = entries;
    }
    
    public List<long> Match(Key key)
    {
        return _entries
            .Where(entry => entry.Value.Keys.Contains(key))
            .Select(entry => entry.Key)
            .ToList();
    }
    
    public static InputContext Empty => new(new Dictionary<long, InputContextEntry>());

    public static Builder<TInputAction> GetBuilder<TInputAction>() where TInputAction : Enum
    {
        return new Builder<TInputAction>();
    }

    public class Builder<TInputAction> where TInputAction : Enum
    {
        private readonly Dictionary<TInputAction, InputContextEntry> _entries = new();

        public Builder<TInputAction> Add(TInputAction action, Key key)
        {
            if (_entries.TryGetValue(action, out var entry))
            {
                entry.Keys.Add(key);
                return this;
            }
            _entries[action] = new InputContextEntry(Convert.ToInt64(action), [key]);
            return this;
        }

        public InputContext Build()
        {
            var dictionaryEntries = _entries.ToDictionary(
                kvp => Convert.ToInt64(kvp.Key),
                kvp => new InputContextEntry(Convert.ToInt64(kvp.Key), kvp.Value.Keys)
            );
            return new InputContext(dictionaryEntries);
        }
    }
}

public class InputContextEntry(long action, List<Key> keys)
{
    public long Action { get; } = action;
    public List<Key> Keys { get; } = keys;
}