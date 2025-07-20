using Silk.NET.GLFW;
using Silk.NET.Input;

namespace Engine.Input;

public partial class InputHandler
{
    public List<IKeyboard> KnownKeyboards { get; } = [];
    
    public readonly Dictionary<Key, List<BoundHeldAction>> OnKeyboardKeyEvent = new();
    public readonly Dictionary<Key, List<BoundHeldAction>> OnKeyboardKeyHeldEvent = new();
    public readonly Dictionary<Key, List<BoundHeldAction>> OnKeyboardKeyReleasedEvent = new();
    
    private readonly HashSet<Key> _heldKeys = [];
    
    public void BindKeyboardEvents(IKeyboard keyboard)
    {
        if (KnownKeyboards.Contains(keyboard))
            return;
        keyboard.KeyDown += OnKeyDown;
        keyboard.KeyUp += OnKeyUp;
        KnownKeyboards.Add(keyboard);
    }
    
    public void UnbindKeyboardEvents()
    {
        foreach (var knownKeyboard in KnownKeyboards)
        {
            knownKeyboard.KeyDown -= OnKeyDown;
            knownKeyboard.KeyUp -= OnKeyUp;
        }
        KnownKeyboards.Clear();
    }

    public bool IsInputHeld(long inputAction)
    {
        var modifiers = GetModifiers();
        foreach (var heldKey in _heldKeys)
        {
            if (CurrentContext.Match(heldKey, modifiers).Contains(inputAction))
            {
                return true;
            }
        }
        foreach (var heldMouseButton in _heldMouseButtons)
        {
            if (CurrentContext.Match(heldMouseButton, modifiers).Contains(inputAction))
            {
                return true;
            }
        }

        return false;
    }
    
    public bool IsKeyHeld(Key key)
    {
        return _heldKeys.Contains(key);
    }
    
    private void OnKeyDown(IKeyboard keyboard, Key key, int num)
    {
        _heldKeys.Add(key);
        
        OnKeyboardKeyEvent.TryGetValue(key, out var boundKeyActionList);
        if (boundKeyActionList != null)
        {
            foreach (var boundAction in boundKeyActionList)
            {
                boundAction.Action.Invoke(boundAction.X, boundAction.Y, boundAction.Z, 0.0f);
            }
        }
            
        var triggeredActions = CurrentContext.Match(key, GetModifiers());
        triggeredActions.ForEach(triggeredActionId =>
        {
            OnInputEvent.TryGetValue(triggeredActionId, out var inputActionList);
            if (inputActionList == null) return;
                
            foreach (var boundAction in inputActionList)
            {
                boundAction.Action.Invoke(boundAction.X, boundAction.Y, boundAction.Z, 0.0f);
            }
        });
    }

    private void OnKeyUp(IKeyboard keyboard, Key key, int num)
    {
        _heldKeys.Remove(key);
        
        OnKeyboardKeyReleasedEvent.TryGetValue(key, out var boundKeyActionList);
        if (boundKeyActionList != null)
        {
            foreach (var boundAction in boundKeyActionList)
            {
                boundAction.Action.Invoke(boundAction.X, boundAction.Y, boundAction.Z, 0.0f);
            }
        }
            
        var triggeredActions = CurrentContext.Match(key, GetModifiers());
        triggeredActions.ForEach(triggeredActionId =>
        {
            OnInputReleasedEvent.TryGetValue(triggeredActionId, out var inputActionList);
            if (inputActionList == null) return;
                
            foreach (var boundAction in inputActionList)
            {
                boundAction.Action.Invoke(boundAction.X, boundAction.Y, boundAction.Z, 0.0f);
            }
        });
    }

    /// <summary>
    /// Process OnKeyHeld and OnInputHeld events for all currently held keys.
    /// </summary>
    /// <returns>List of triggered input actions (by long representation of the bound enum)</returns>
    private void SendHeldKeyboardEvents(List<KeyModifiers> modifiers, ref Dictionary<string, List<BoundHeldAction>> triggeredHandlers)
    {
        foreach (var heldKey in _heldKeys)
        {
            OnKeyboardKeyHeldEvent.TryGetValue(heldKey, out var boundKeyActionList);
            if (boundKeyActionList != null)
            {
                foreach (var boundAction in boundKeyActionList)
                {
                    if (!triggeredHandlers.ContainsKey(boundAction.GroupId))
                        triggeredHandlers[boundAction.GroupId] = [];
                    triggeredHandlers[boundAction.GroupId].Add(boundAction);
                }
            }

            var triggeredActions = CurrentContext.Match(heldKey, modifiers);
            foreach (var triggeredActionId in triggeredActions)
            {
                OnInputHeldEvent.TryGetValue(triggeredActionId, out var boundActionList);
                if (boundActionList == null) continue;
                
                foreach (var boundAction in boundActionList)
                {
                    if (!triggeredHandlers.ContainsKey(boundAction.GroupId))
                        triggeredHandlers[boundAction.GroupId] = [];
                    triggeredHandlers[boundAction.GroupId].Add(boundAction);
                }
            }
        }
    }
}