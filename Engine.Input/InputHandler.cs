using System.Numerics;
using Engine.Input.Contexts;
using Silk.NET.Input;

namespace Engine.Input;

public class InputHandler
{
    private readonly HashSet<Key> _heldKeys = [];
    InputContext _currentContext = InputContext.Empty;
    
    private static InputHandler? _instance;
    public static InputHandler GetInstance()
    {
        return _instance ??= new InputHandler();
    }

    public static void SetInputContext(InputContext context)
    {
        GetInstance()._currentContext = context;
    }
    
    public void BindKeyboardEvents(IKeyboard keyboard)
    {
        keyboard.KeyDown += (_, key, _) =>
        {
            OnKeyboardKeyEvent.TryGetValue(key, out var keyAction);
            keyAction?.Invoke();
            
            var triggeredActions = _currentContext.Match(key);
            triggeredActions.ForEach(triggeredActionId =>
            {
                OnInputEvent.TryGetValue(triggeredActionId, out var inputAction);
                inputAction?.Invoke();
            });
            
            _heldKeys.Add(key);
        };

        keyboard.KeyUp += (_, key, _) =>
        {
            OnKeyboardKeyReleasedEvent.TryGetValue(key, out var action);
            action?.Invoke();
            
            var triggeredActions = _currentContext.Match(key);
            triggeredActions.ForEach(triggeredActionId =>
            {
                OnInputReleasedEvent.TryGetValue(triggeredActionId, out var inputAction);
                inputAction?.Invoke();
            });
            
            _heldKeys.Remove(key);
        };
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

            var triggeredActions = _currentContext.Match(heldKey);
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
                parameterSum.Z += action.Z;
            }
            handler[0].Action(deltaTime, parameterSum.X, parameterSum.Y, parameterSum.Z);
            
        }
    }
    
    public void BindMouseEvents(IMouse mouse)
    {
    }

    public static void ClearSubscriptions(object owner)
    {
        // TODO: Also clear subscription for keydown/keyup events
        
        Prune(OnInputHeldEvent,       owner);   // key = long
        Prune(OnKeyboardKeyHeldEvent, owner);   // key = Key
        return;

        static void Prune<TKey>(IDictionary<TKey, List<BoundHeldAction>> dict, object owner)
        {
            foreach (var list in dict.Values)
            {
                list.RemoveAll(a => ReferenceEquals(a.Owner, owner));
            }
        }
    }
    
    public static readonly Dictionary<long, Action> OnInputEvent = new();
    public static readonly Dictionary<long, List<BoundHeldAction>> OnInputHeldEvent = new();
    public static readonly Dictionary<long, Action> OnInputReleasedEvent = new();
    public static readonly Dictionary<Key, Action> OnKeyboardKeyEvent = new();
    public static readonly Dictionary<Key, List<BoundHeldAction>> OnKeyboardKeyHeldEvent = new();
    public static readonly Dictionary<Key, Action> OnKeyboardKeyReleasedEvent = new();
}

public struct BoundHeldAction(object owner, string groupId, double x, double y, double z, Action<double, double, double, double> action)
{
    public readonly object Owner = owner;
    public readonly string GroupId = groupId;
    public readonly double X = x;
    public readonly double Y = y;
    public readonly double Z = z;
    public readonly Action<double, double, double, double> Action = action;
}
