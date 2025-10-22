using Silk.NET.GLFW;
using Silk.NET.Input;
using MouseButton = Silk.NET.Input.MouseButton;

namespace Engine.Core.Input.Contexts;

public class InputContext
{
    private readonly Dictionary<long, List<InputContextEntry>> _entries;

    private InputContext(Dictionary<long, List<InputContextEntry>> entries)
    {
        _entries = entries;
    }
    
    public List<long> Match(Key key, List<KeyModifiers> modifiers)
    {
        return _entries.Values
            .SelectMany(list => list)
            .Where(entry => entry.Keys.Contains(key) && entry.Modifiers.All(modifiers.Contains) && entry.Modifiers.Count == modifiers.Count)
            .Select(entry => entry.Action)
            .ToList();
    }
    
    public List<long> Match(MouseAxis axis, List<KeyModifiers> modifiers)
    {
        return _entries.Values
            .SelectMany(list => list)
            .Where(entry => entry.MouseAxes.Contains(axis) && entry.Modifiers.All(modifiers.Contains) && entry.Modifiers.Count == modifiers.Count)
            .Select(entry => entry.Action)
            .ToList();
    }

    public List<long> Match(MouseButton button, List<KeyModifiers> modifiers)
    {
        return _entries.Values
            .SelectMany(list => list)
            .Where(entry => entry.MouseButtons.Contains(button) && entry.Modifiers.All(modifiers.Contains) && entry.Modifiers.Count == modifiers.Count)
            .Select(entry => entry.Action)
            .ToList();
    }

    public List<long> Match(Button button)
    {
        return _entries.Values
            .SelectMany(list => list)
            .Where(entry => entry.GamepadButtons.Contains(button.Name))
            .Select(entry => entry.Action)
            .ToList();
    }
    
    public List<long> Match(GamepadAnalog analog)
    {
        return _entries.Values
            .SelectMany(list => list)
            .Where(entry => entry.GamepadAnalogs.Contains(analog))
            .Select(entry => entry.Action)
            .ToList();
    }
    
    public InputContext Combine(InputContext other)
    {
        var combinedEntries = new Dictionary<long, List<InputContextEntry>>(_entries);
        
        foreach (var entry in other._entries)
        {
            var sourceEntry = combinedEntries.GetValueOrDefault(entry.Key, []);
            sourceEntry.AddRange(entry.Value);
            combinedEntries[entry.Key] = sourceEntry;
        }
        
        return new InputContext(combinedEntries);
    }
    
    public static InputContext Empty => new(new Dictionary<long, List<InputContextEntry>>());

    public static InputContext From(InputContext inputContext)
    {
        var copiedEntries = new Dictionary<long, List<InputContextEntry>>();
    
        foreach (var kvp in inputContext._entries)
        {
            copiedEntries[kvp.Key] = kvp.Value;
        }
    
        return new InputContext(copiedEntries);
    }

    public static Builder<TInputAction> GetBuilder<TInputAction>() where TInputAction : System.Enum
    {
        return new Builder<TInputAction>();
    }

    public class Builder<TInputAction> where TInputAction : System.Enum
    {
        private readonly Dictionary<TInputAction, List<InputContextEntry>> _entries = new();

        public Builder<TInputAction> Add(TInputAction action, Key key, List<KeyModifiers>? modifiers = null)
        {
            if (_entries.TryGetValue(action, out var list))
            {
                list.Add(new InputContextEntry(Convert.ToInt64(action))
                {
                    Keys = [key],
                    Modifiers = modifiers ?? []
                });
                return this;
            }

            _entries[action] = [new InputContextEntry(Convert.ToInt64(action))
            {
                Keys = [key],
                Modifiers = modifiers ?? []
            }];
            return this;
        }

        public Builder<TInputAction> Add(TInputAction action, MouseAxis axis, List<KeyModifiers>? modifiers = null)
        {
            if (_entries.TryGetValue(action, out var list))
            {
                list.Add(new InputContextEntry(Convert.ToInt64(action))
                {
                    MouseAxes = [axis],
                    Modifiers = modifiers ?? []
                });
                return this;
            }

            _entries[action] = [new InputContextEntry(Convert.ToInt64(action))
            {
                MouseAxes = [axis],
                Modifiers = modifiers ?? []
            }];
            return this;
        }
        
        public Builder<TInputAction> Add(TInputAction action, MouseButton button, List<KeyModifiers>? modifiers = null)
        {
            if (_entries.TryGetValue(action, out var list))
            {
                list.Add(new InputContextEntry(Convert.ToInt64(action))
                {
                    MouseButtons = [button],
                    Modifiers = modifiers ?? []
                });
                return this;
            }

            _entries[action] = [new InputContextEntry(Convert.ToInt64(action))
            {
                MouseButtons = [button],
                Modifiers = modifiers ?? []
            }];
            return this;
        }

        public Builder<TInputAction> Add(TInputAction action, ButtonName button)
        {
            if (_entries.TryGetValue(action, out var list))
            {
                list.Add(new InputContextEntry(Convert.ToInt64(action))
                {
                    GamepadButtons = [button],
                });
                return this;
            }

            _entries[action] = [new InputContextEntry(Convert.ToInt64(action))
            {
                GamepadButtons = [button],
            }];
            return this;
        }
        
        public Builder<TInputAction> Add(TInputAction action, GamepadAnalog analog)
        {
            if (_entries.TryGetValue(action, out var list))
            {
                list.Add(new InputContextEntry(Convert.ToInt64(action))
                {
                    GamepadAnalogs = [analog],
                });
                return this;
            }

            _entries[action] = [new InputContextEntry(Convert.ToInt64(action))
            {
                GamepadAnalogs = [analog],
            }];
            return this;
        }

        public InputContext Build()
        {
            var dictionaryEntries = _entries.ToDictionary(
                kvp => Convert.ToInt64(kvp.Key),
                kvp => kvp.Value
            );
            return new InputContext(dictionaryEntries);
        }
    }
}

public readonly struct InputContextEntry(long action)
{
    public long Action { get; } = action;
    public List<Key> Keys { get; internal init; } = [];
    public List<MouseAxis> MouseAxes { get; internal init; } = [];
    public List<MouseButton> MouseButtons { get; internal init; } = [];
    public List<ButtonName> GamepadButtons { get; internal init; } = [];
    public List<GamepadAnalog> GamepadAnalogs { get; internal init; } = [];
    public List<KeyModifiers> Modifiers { get; internal init; } = [];
}