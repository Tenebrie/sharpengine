using Silk.NET.GLFW;
using Silk.NET.Input;
using MouseButton = Silk.NET.Input.MouseButton;

namespace Engine.Core.Input.Contexts;

public class InputContext
{
    private readonly Dictionary<long, InputContextEntry> _entries;

    private InputContext(Dictionary<long, InputContextEntry> entries)
    {
        _entries = entries;
    }
    
    public List<long> Match(Key key, List<KeyModifiers> modifiers)
    {
        return _entries.Values
            .Where(entry => entry.Keys.Contains(key) && 
                            (modifiers.Count == 0 || 
                             modifiers.All(m => entry.Modifiers.Contains(m))))
            .Select(entry => entry.Action)
            .ToList();
    }
    
    public List<long> Match(MouseAxis axis, List<KeyModifiers> modifiers)
    {
        return _entries.Values
            .Where(entry => entry.MouseAxes.Contains(axis) && 
                            (modifiers.Count == 0 || 
                             modifiers.All(m => entry.Modifiers.Contains(m))))
            .Select(entry => entry.Action)
            .ToList();
    }

    public List<long> Match(MouseButton button, List<KeyModifiers> modifiers)
    {
        return _entries.Values
            .Where(entry => entry.MouseButtons.Contains(button) && 
                            (modifiers.Count == 0 || 
                             modifiers.All(m => entry.Modifiers.Contains(m))))
            .Select(entry => entry.Action)
            .ToList();
    }
    
    public InputContext Combine(InputContext other)
    {
        var combinedEntries = new Dictionary<long, InputContextEntry>(_entries);
        
        foreach (var entry in other._entries)
        {
            if (combinedEntries.TryGetValue(entry.Key, out var existingEntry))
            {
                existingEntry.Keys.AddRange(entry.Value.Keys);
            }
            else
            {
                combinedEntries[entry.Key] = entry.Value;
            }
        }
        
        return new InputContext(combinedEntries);
    }
    
    public static InputContext Empty => new(new Dictionary<long, InputContextEntry>());

    public static InputContext From(InputContext inputContext)
    {
        var copiedEntries = new Dictionary<long, InputContextEntry>();
    
        foreach (var kvp in inputContext._entries)
        {
            copiedEntries[kvp.Key] = new InputContextEntry(kvp.Key)
            {
                Keys = [..kvp.Value.Keys],
                MouseAxes = [..kvp.Value.MouseAxes],
                MouseButtons = [..kvp.Value.MouseButtons],
                Modifiers = [..kvp.Value.Modifiers],
            };
        }
    
        return new InputContext(copiedEntries);
    }

    public static Builder<TInputAction> GetBuilder<TInputAction>() where TInputAction : System.Enum
    {
        return new Builder<TInputAction>();
    }

    public class Builder<TInputAction> where TInputAction : System.Enum
    {
        private readonly Dictionary<TInputAction, InputContextEntry> _entries = new();

        public Builder<TInputAction> Add(TInputAction action, Key key, List<KeyModifiers>? modifiers = null)
        {
            if (_entries.TryGetValue(action, out var entry))
            {
                entry.Keys.Add(key);
                return this;
            }

            _entries[action] = new InputContextEntry(Convert.ToInt64(action))
            {
                Keys = [key],
            };
            return this;
        }

        public Builder<TInputAction> Add(TInputAction action, MouseAxis axis, List<KeyModifiers>? modifiers = null)
        {
            if (_entries.TryGetValue(action, out var entry))
            {
                entry.MouseAxes.Add(axis);
                return this;
            }

            _entries[action] = new InputContextEntry(Convert.ToInt64(action))
            {
                MouseAxes = [axis]
            };
            return this;
        }
        
        public Builder<TInputAction> Add(TInputAction action, MouseButton button, List<KeyModifiers>? modifiers = null)
        {
            if (_entries.TryGetValue(action, out var entry))
            {
                entry.MouseButtons.Add(button);
                return this;
            }

            _entries[action] = new InputContextEntry(Convert.ToInt64(action))
            {
                MouseButtons = [button]
            };
            return this;
        }

        public InputContext Build()
        {
            var dictionaryEntries = _entries.ToDictionary(
                kvp => Convert.ToInt64(kvp.Key),
                kvp => new InputContextEntry(Convert.ToInt64(kvp.Key))
                {
                    Keys = kvp.Value.Keys,
                    MouseAxes = kvp.Value.MouseAxes,
                    MouseButtons = kvp.Value.MouseButtons,
                    Modifiers = kvp.Value.Modifiers
                }
            );
            return new InputContext(dictionaryEntries);
        }
    }
}

public class InputContextEntry(long action)
{
    public long Action { get; } = action;
    public List<Key> Keys { get; internal set; } = [];
    public List<MouseAxis> MouseAxes { get; internal set; } = [];
    public List<MouseButton> MouseButtons { get; internal set; } = [];
    public List<KeyModifiers> Modifiers { get; internal set; } = [];
}