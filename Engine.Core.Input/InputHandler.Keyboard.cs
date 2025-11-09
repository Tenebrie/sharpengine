using Engine.Core.Common;
using Engine.Core.Communication.Tasks;
using Engine.Core.Logging;
using Engine.Core.Modules.EntitySystem;
using Silk.NET.GLFW;
using Silk.NET.Input;

namespace Engine.Core.Input;

public partial class InputHandler
{
    public List<IKeyboard> KnownKeyboards { get; } = [];
    
    public readonly Dictionary<Key, List<BoundHeldAction>> OnKeyboardKeyEvent = new();
    public readonly Dictionary<(IBackstage identity, Key key), List<BoundHeldAction>> OnKeyboardKeyHeldEvent = new();
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
        GameThreadTask.Run(() => OnKeyDownHandler(key));
    }
    private void OnKeyDownHandler(Key key)
    {
        _heldKeys.Add(key);
        UserInputMode = UserInputMode.KeyboardAndMouse;
        
        OnKeyboardKeyEvent.TryGetValue(key, out var boundKeyActionList);
        if (boundKeyActionList != null)
        {
            foreach (var boundAction in boundKeyActionList)
            {
                try
                {
                    boundAction.Action.Invoke(boundAction.X, boundAction.Y, boundAction.Z, 0.0f);
                } catch (Exception e)
                {
                    Logger.Error("Error in OnKeyInput: " + e.Message, e);
                }
            }
        }
            
        var triggeredActions = CurrentContext.Match(key, GetModifiers());
        TriggerOnInputEngaged(triggeredActions, Vector3.One);
    }

    private void OnKeyUp(IKeyboard keyboard, Key key, int num)
    {
        GameThreadTask.Run(() => OnKeyUpHandler(key));
    }
    
    private void OnKeyUpHandler(Key key)
    {
        _heldKeys.Remove(key); 
        
        OnKeyboardKeyReleasedEvent.TryGetValue(key, out var boundKeyActionList);
        if (boundKeyActionList != null)
        {
            foreach (var boundAction in boundKeyActionList)
            {
                try 
                {
                    boundAction.Action.Invoke(boundAction.X, boundAction.Y, boundAction.Z, 0.0f);
                }
                catch (Exception e)
                {
                    Logger.Error("Error in OnKeyReleased: " + e.Message, e);
                }
            }
        }
            
        var triggeredActions = CurrentContext.Match(key, GetModifiers());
        TriggerOnInputReleased(triggeredActions, Vector3.One);
    }

    /// <summary>
    /// Process OnKeyHeld and OnInputHeld events for all currently held keys.
    /// </summary>
    /// <returns>List of triggered input actions (by long representation of the bound enum)</returns>
    private void SendHeldKeyboardEvents(IBackstage identity, List<KeyModifiers> modifiers, ref Dictionary<string, List<BoundHeldAction>> triggeredHandlers)
    {
        foreach (var heldKey in _heldKeys)
        {
            OnKeyboardKeyHeldEvent.TryGetValue((identity, heldKey), out var boundKeyActionList);
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
            TriggerOnInputHeld(identity, triggeredActions, Vector3.One, ref triggeredHandlers);
        }
    }
}