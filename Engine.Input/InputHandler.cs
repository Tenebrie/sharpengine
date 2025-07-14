using System.Numerics;
using Engine.Input.Contexts;
using Silk.NET.Input;

namespace Engine.Input;

public class InputHandler
{
    private readonly HashSet<Key> _heldKeys = [];
    public InputContext CurrentContext { get; set; } = InputContext.Empty;
    public List<IKeyboard> KnownKeyboards { get; } = [];
    
    public void BindMouseEvents(IMouse mouse)
    {
    }
    
    public void BindKeyboardEvents(IKeyboard keyboard)
    {
        if (KnownKeyboards.Contains(keyboard))
            return;
        keyboard.KeyDown += OnKeyDown;
        keyboard.KeyUp += OnKeyUp;
        KnownKeyboards.Add(keyboard);
    }
    
    public void ClearKeyboardEvents()
    {
        foreach (var knownKeyboard in KnownKeyboards)
        {
            knownKeyboard.KeyDown -= OnKeyDown;
            knownKeyboard.KeyUp -= OnKeyUp;
        }
    }

    private void OnKeyDown(IKeyboard keyboard, Key key, int num)
    {
        OnKeyboardKeyEvent.TryGetValue(key, out var boundKeyActionList);
        if (boundKeyActionList != null)
        {
            foreach (var boundAction in boundKeyActionList)
            {
                boundAction.Action.Invoke(boundAction.X, boundAction.Y, 0.0f);
            }
        }
            
        var triggeredActions = CurrentContext.Match(key);
        triggeredActions.ForEach(triggeredActionId =>
        {
            OnInputEvent.TryGetValue(triggeredActionId, out var inputActionList);
            if (inputActionList == null) return;
                
            foreach (var boundAction in inputActionList)
            {
                boundAction.Action.Invoke(boundAction.X, boundAction.Y, 0.0f);
            }
        });
            
        _heldKeys.Add(key);
    }

    private void OnKeyUp(IKeyboard keyboard, Key key, int num)
    {
        OnKeyboardKeyReleasedEvent.TryGetValue(key, out var boundKeyActionList);
        if (boundKeyActionList != null)
        {
            foreach (var boundAction in boundKeyActionList)
            {
                boundAction.Action.Invoke(boundAction.X, boundAction.Y, 0.0f);
            }
        }
            
        var triggeredActions = CurrentContext.Match(key);
        triggeredActions.ForEach(triggeredActionId =>
        {
            OnInputReleasedEvent.TryGetValue(triggeredActionId, out var inputActionList);
            if (inputActionList == null) return;
                
            foreach (var boundAction in inputActionList)
            {
                boundAction.Action.Invoke(boundAction.X, boundAction.Y, 0.0f);
            }
        });
            
        _heldKeys.Remove(key);
    }

    public void SendKeyboardHeldEvents(double deltaTime)
    {
        Dictionary<string, List<BoundHeldAction>> triggeredHandlers = new();
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

            var triggeredActions = CurrentContext.Match(heldKey);
            triggeredActions.ForEach(triggeredActionId =>
            {
                OnInputHeldEvent.TryGetValue(triggeredActionId, out var boundActionList);
                if (boundActionList == null) return;
                
                foreach (var boundAction in boundActionList)
                {
                    if (!triggeredHandlers.ContainsKey(boundAction.GroupId))
                        triggeredHandlers[boundAction.GroupId] = [];
                    triggeredHandlers[boundAction.GroupId].Add(boundAction);
                }
            });
        }
        foreach (var handler in triggeredHandlers.Values)
        {
            var parameterSum = new Vector();
            foreach (var action in handler)
            {
                parameterSum.X += action.X;
                parameterSum.Y += action.Y;
            }
            handler[0].Action(deltaTime, parameterSum.X, parameterSum.Y);
            
        }
    }

    public void ClearSubscriptions(object owner)
    {
        Prune(OnInputEvent, owner);
        Prune(OnInputHeldEvent, owner);
        Prune(OnInputReleasedEvent, owner);
        Prune(OnKeyboardKeyEvent, owner);
        Prune(OnKeyboardKeyHeldEvent, owner);
        Prune(OnKeyboardKeyReleasedEvent, owner);
        return;

        static void Prune<TKey>(IDictionary<TKey, List<BoundHeldAction>> dict, object owner)
        {
            foreach (var list in dict.Values)
            {
                list.RemoveAll(a => ReferenceEquals(a.Owner, owner));
            }
        }
    }
    
    public readonly Dictionary<long, List<BoundHeldAction>> OnInputEvent = new();
    public readonly Dictionary<long, List<BoundHeldAction>> OnInputHeldEvent = new();
    public readonly Dictionary<long, List<BoundHeldAction>> OnInputReleasedEvent = new();
    public readonly Dictionary<Key, List<BoundHeldAction>> OnKeyboardKeyEvent = new();
    public readonly Dictionary<Key, List<BoundHeldAction>> OnKeyboardKeyHeldEvent = new();
    public readonly Dictionary<Key, List<BoundHeldAction>> OnKeyboardKeyReleasedEvent = new();
}

public struct BoundHeldAction(object owner, string groupId, double x, double y, Action<double, double, double> action)
{
    public readonly object Owner = owner;
    public readonly string GroupId = groupId;
    public readonly double X = x;
    public readonly double Y = y;
    public readonly Action<double, double, double> Action = action;
}
