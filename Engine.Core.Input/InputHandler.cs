using Engine.Core.Common;
using Engine.Core.Enum;
using Engine.Core.Input.Contexts;
using Engine.Core.Logging;
using Engine.Core.Modules;
using Engine.Core.Modules.EntitySystem;
using Silk.NET.GLFW;
using Silk.NET.Input;
using MouseButton = Silk.NET.Input.MouseButton;

namespace Engine.Core.Input;

public enum UserInputMode
{
    KeyboardAndMouse,
    Gamepad
}

public class InputContextList(IRootHypervisor hypervisor)
{
    internal record struct InputContextEntry(
        IInputContextProvider Provider,
        object Identity,
        InputContext InputContext);

    internal readonly List<InputContextEntry> ContextEntries = [];
    
    private List<InputContextEntry> ActiveContextEntries => ContextEntries
        .Where(entry => entry.Provider.RunsInGameplayContext.HasFlag(hypervisor.GameplayContext))
        .ToList();
    
    public List<long> Match(Key key, List<KeyModifiers> modifiers) =>
        ActiveContextEntries.SelectMany(entry => entry.InputContext.Match(key, modifiers)).ToList();

    public List<long> Match(MouseAxis axis, List<KeyModifiers> modifiers) =>
        ActiveContextEntries.SelectMany(entry => entry.InputContext.Match(axis, modifiers)).ToList();

    public List<long> Match(MouseButton button, List<KeyModifiers> modifiers) =>
        ActiveContextEntries.SelectMany(entry => entry.InputContext.Match(button, modifiers)).ToList();

    public List<long> Match(Button button) =>
        ActiveContextEntries.SelectMany(entry => entry.InputContext.Match(button)).ToList();
    
    public List<long> Match(GamepadAnalog analog) =>
        ActiveContextEntries.SelectMany(entry => entry.InputContext.Match(analog)).ToList();
}

public partial class InputHandler(IRootHypervisor hypervisor)
{
    private InputContextList CurrentContext { get; set; } = new(hypervisor);
    
    private Dictionary<string, List<BoundHeldAction>> _triggeredHandlers = new();
    
    public UserInputMode UserInputMode { get; set; } = UserInputMode.KeyboardAndMouse;

    public void SetInputContext(IInputContextProvider provider, object identity, InputContext inputContext)
    {
        CurrentContext.ContextEntries.RemoveAll(entry => entry.Identity == identity);
        CurrentContext.ContextEntries.Add(new InputContextList.InputContextEntry
        {
            Provider = provider,
            Identity = identity,
            InputContext = inputContext,
        });
    }
    public void RemoveInputContext(object identity)
    {
        CurrentContext.ContextEntries.RemoveAll(entry => entry.Identity == identity);
    }
    public void PurgeAll(IInputContextProvider provider)
    {
        CurrentContext.ContextEntries.RemoveAll(entry => entry.Provider == provider);
        _mouseCursorModifiers.RemoveAll(entry => entry.Provider == provider);
        RecalculateMouseCursor();
        
        if (CurrentContext.ContextEntries.Count != 0)
            return;
        
        UnbindMouseEvents();
        UnbindGamepadEvents();
        UnbindKeyboardEvents();
    }
    
    public void SendHeldInputEvents(IBackstage identity, double deltaTime)
    {
        var modifiers = GetModifiers();
        SendHeldKeyboardEvents(identity, modifiers, ref _triggeredHandlers);
        SendHeldMouseButtonEvents(identity, modifiers, ref _triggeredHandlers);
        SendHeldGamepadButtonEvents(identity, ref _triggeredHandlers);
        
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
        if (values is { X: 0, Y: 0, Z: 0 })
            return;
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

    private void TriggerOnInputHeld(IBackstage identity, List<long> triggeredActions, Vector3 values, ref Dictionary<string, List<BoundHeldAction>> triggeredHandlers)
    {
        foreach (var triggeredActionId in triggeredActions)
        {
            OnInputHeldEvent.TryGetValue((identity, triggeredActionId), out var boundActionList);
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
    public readonly Dictionary<(IBackstage identity, long actionId), List<BoundHeldAction>> OnInputHeldEvent = new();
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

public interface IInputContextProvider
{
    public GameplayContext RunsInGameplayContext { get; }
}
