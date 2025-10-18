using Engine.Core.Common;
using Engine.Core.Input.Contexts;
using Engine.Core.Logging;
using Silk.NET.GLFW;
using Silk.NET.Input;

namespace Engine.Core.Input;

public enum UserInputMode
{
    KeyboardAndMouse,
    Gamepad
}

public partial class InputHandler
{
    public InputContext CurrentContext { get; set; } = InputContext.Empty;
    
    private Dictionary<string, List<BoundHeldAction>> _triggeredHandlers = new();
    
    public UserInputMode UserInputMode { get; set; } = UserInputMode.KeyboardAndMouse;
    
    public void SendHeldInputEvents(double deltaTime)
    {
        var modifiers = GetModifiers();
        SendHeldKeyboardEvents(modifiers, ref _triggeredHandlers);
        SendHeldMouseButtonEvents(modifiers, ref _triggeredHandlers);
        SendHeldGamepadButtonEvents(ref _triggeredHandlers);
        
        foreach (var handler in _triggeredHandlers.Values)
        {
            var parameterSum = new Vector3();
            foreach (var action in handler)
            {   
                parameterSum.X += action.X;
                parameterSum.Y += action.Y;
                parameterSum.Z += action.Z;
            }
            try 
            {
                handler[0].Action(deltaTime, parameterSum.X, parameterSum.Y, parameterSum.Z);
            }
            catch (Exception e)
            {
                Logger.Error($"Error in OnInputHeld: {e.Message}", e);
            }
        }
        _triggeredHandlers.Clear();
    }
    
    private void TriggerOnInputEngaged(List<long> actions, Vector3 values)
    {
        foreach (var triggeredActionId in actions)
        {
            OnInputEvent.TryGetValue(triggeredActionId, out var inputActionList);
            if (inputActionList == null)
                continue;
                
            foreach (var boundAction in inputActionList)
            {
                try 
                {
                    boundAction.Action.Invoke(values.X * boundAction.X, values.Y * boundAction.Y, values.Z * boundAction.Z, 0.0);
                }
                catch (Exception e)
                {
                    Logger.Error($"Error in OnInput: {e.Message}", e);
                }
            }
        }
    }
    
    private void TriggerOnInputReleased(List<long> actions, Vector3 values)
    {
        foreach (var triggeredActionId in actions)
        {
            OnInputReleasedEvent.TryGetValue(triggeredActionId, out var inputActionList);
            if (inputActionList == null)
                continue;
                
            foreach (var boundAction in inputActionList)
            {
                try
                {
                    boundAction.Action.Invoke(values.X * boundAction.X, values.Y * boundAction.Y, values.Z * boundAction.Z, 0.0);
                }
                catch (Exception e)
                {
                    Logger.Error($"Error in OnInputReleased: {e.Message}", e);
                }
            }
        }
    }

    private void TriggerOnInputHeld(List<long> triggeredActions, Vector3 values, ref Dictionary<string, List<BoundHeldAction>> triggeredHandlers)
    {
        foreach (var triggeredActionId in triggeredActions)
        {
            OnInputHeldEvent.TryGetValue(triggeredActionId, out var boundActionList);
            if (boundActionList == null) continue;
                
            foreach (var boundAction in boundActionList)
            {
                if (!triggeredHandlers.ContainsKey(boundAction.GroupId))
                    triggeredHandlers[boundAction.GroupId] = [];
                triggeredHandlers[boundAction.GroupId].Add(new BoundHeldAction(boundAction, values));
            }
        }
    }

    private List<KeyModifiers> GetModifiers()
    {
        List<KeyModifiers> modifiers = [];
        if (IsKeyHeld(Key.ShiftLeft) || IsKeyHeld(Key.ShiftRight))
            modifiers.Add(KeyModifiers.Shift);
        if (IsKeyHeld(Key.ControlLeft) || IsKeyHeld(Key.ControlRight))
            modifiers.Add(KeyModifiers.Control);
        if (IsKeyHeld(Key.AltLeft) || IsKeyHeld(Key.AltRight))
            modifiers.Add(KeyModifiers.Alt);
        return modifiers;
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
}

public struct BoundHeldAction(object owner, string groupId, double x, double y, double z, Action<double, double, double, double> action)
{
    public readonly object Owner = owner;
    public readonly string GroupId = groupId;
    public readonly double X = x;
    public readonly double Y = y;
    public readonly double Z = z;
    public readonly Action<double, double, double, double> Action = action;
    
    public BoundHeldAction(BoundHeldAction other, Vector3 arguments)
        : this(other.Owner, other.GroupId, other.X, other.Y, other.Z, other.Action)
    {
        X = other.X * arguments.X;
        Y = other.Y * arguments.Y;
        Z = other.Z * arguments.Z;
    }
}
