using Silk.NET.Input;

namespace Engine.Input;

public class InputHandler
{
    private readonly HashSet<Key> _heldKeys = [];
    public void BindKeyboardEvents(IKeyboard keyboard)
    {
        keyboard.KeyDown += (_, key, _) =>
        {
            if (OnKeyboardEvent.TryGetValue(key, out var subscriberList))
            {
                foreach (var actionWrapper in subscriberList)
                {
                    actionWrapper.Action.Invoke(actionWrapper.Value);
                }
            }
            
            _heldKeys.Add(key);
        };

        keyboard.KeyUp += (_, key, _) =>
        {
            if (OnKeyboardReleasedEvent.TryGetValue(key, out var subscriberList))
            {
                foreach (var actionWrapper in subscriberList)
                {
                    actionWrapper.Action.Invoke(actionWrapper.Value);
                }
            }
            
            _heldKeys.Remove(key);
        };
    }

    public void SendKeyboardHeldEvents(double deltaTime)
    {
        foreach (var heldKey in _heldKeys)
        {
            if (!OnKeyboardHeldEvent.TryGetValue(heldKey, out var subscriberList)) continue;
            foreach (var actionWrapper in subscriberList)
            {
                actionWrapper.Action.Invoke(deltaTime, actionWrapper.Value);
            }
        }
    }
    
    public void BindMouseEvents(IMouse mouse)
    {
    }

    public static readonly Dictionary<Key, List<KeyboardEventAction>> OnKeyboardEvent = new();
    public static readonly Dictionary<Key, List<KeyboardHeldEventAction>> OnKeyboardHeldEvent = new();
    public static readonly Dictionary<Key, List<KeyboardEventAction>> OnKeyboardReleasedEvent = new();
}

public struct KeyboardEventAction(Action<double> action, double value)
{
    public readonly Action<double> Action = action;
    public readonly double Value = value;
}

public struct KeyboardHeldEventAction(Action<double, double> action, double value)
{
    public readonly Action<double, double> Action = action;
    public readonly double Value = value;
}
