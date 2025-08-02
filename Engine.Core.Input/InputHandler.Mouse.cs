using Engine.Core.Common;
using Engine.Core.Logging;
using Silk.NET.GLFW;
using Silk.NET.Input;
using MouseButton = Silk.NET.Input.MouseButton;

namespace Engine.Core.Input;

public enum MouseAxis
{
    MoveX,
    MoveY,
    WheelX,
    WheelY
}

public partial class InputHandler
{
    private List<IMouse> KnownMice { get; } = [];
    
    private readonly HashSet<MouseButton> _heldMouseButtons = [];
    private Vector2 _lastMousePosition = Vector2.Zero;

    public Vector2 GetMousePosition()
    {
        if (KnownMice.Count == 0)
            return Vector2.Zero;
        
        return KnownMice[0].Position;
    }
    public void SetMousePosition(Vector2 position)
    {
        _lastMousePosition = position;
        foreach (var knownMouse in KnownMice)
        {
            knownMouse.Position = new System.Numerics.Vector2((float)position.X, (float)position.Y);
        }
    }

    public void SetMouseCursor(StandardCursor cursor)
    {
        foreach (var knownMouse in KnownMice)
        {
            knownMouse.Cursor.StandardCursor = cursor;
        }
    }
    
    public void SetMouseCursorMode(CursorMode mode)
    {
        foreach (var knownMouse in KnownMice)
        {
            knownMouse.Cursor.CursorMode = mode;
        }
    }
    
    public void BindMouseEvents(IMouse mouse)
    {
        if (KnownMice.Contains(mouse))
            return;
        
        mouse.MouseMove += OnMouseMove;
        mouse.MouseDown += OnMouseButtonDown;
        mouse.MouseUp += OnMouseButtonUp;
        mouse.Scroll += OnMouseScroll;
        KnownMice.Add(mouse);
    }
    
    public void UnbindMouseEvents()
    {
        foreach (var mouse in KnownMice)
        {
            mouse.MouseMove -= OnMouseMove;
            mouse.MouseDown -= OnMouseButtonDown;
            mouse.MouseUp -= OnMouseButtonUp;
            mouse.Scroll -= OnMouseScroll;
        }
        KnownMice.Clear();
    }
    
    public bool IsMouseButtonHeld(MouseButton button)
    {
        return _heldMouseButtons.Contains(button);
    }

    private void OnMouseMove(IMouse mouse, System.Numerics.Vector2 position)
    {
        var deltaX = position.X - _lastMousePosition.X;
        var deltaY = position.Y - _lastMousePosition.Y;
        _lastMousePosition = new Vector2(position.X, position.Y);
        var modifiers = GetModifiers();
        var triggeredActions = CurrentContext.Match(MouseAxis.MoveX, modifiers);
        triggeredActions.ForEach(triggeredActionId =>
        {
            OnInputEvent.TryGetValue(triggeredActionId, out var inputActionList);
            if (inputActionList == null) return;
                
            foreach (var boundAction in inputActionList)
            {
                try 
                {
                    boundAction.Action.Invoke(deltaX * boundAction.X, deltaX * boundAction.Y, deltaX * boundAction.Z, 0.0f);
                }
                catch (Exception e)
                {
                    Logger.Error($"Error in OnInput: {e.Message}", e);
                }
            }
        });
        
        triggeredActions = CurrentContext.Match(MouseAxis.MoveY, modifiers);
        triggeredActions.ForEach(triggeredActionId =>
        {
            OnInputEvent.TryGetValue(triggeredActionId, out var inputActionList);
            if (inputActionList == null) return;
                
            foreach (var boundAction in inputActionList)
            {
                try 
                {
                    boundAction.Action.Invoke(deltaY * boundAction.X, deltaY * boundAction.Y, deltaY * boundAction.Z, 0.0f);
                }
                catch (Exception e)
                {
                    Logger.Error($"Error in OnInput: {e.Message}", e);
                }
            }
        });
    }
    
    private void OnMouseScroll(IMouse mouse, ScrollWheel delta)
    {
        var modifiers = GetModifiers();
        var triggeredActions = CurrentContext.Match(MouseAxis.WheelX, modifiers);
        triggeredActions.ForEach(triggeredActionId =>
        {
            OnInputEvent.TryGetValue(triggeredActionId, out var inputActionList);
            if (inputActionList == null) return;
                
            foreach (var boundAction in inputActionList)
            {
                try 
                {
                    boundAction.Action.Invoke(delta.X * boundAction.X, delta.X * boundAction.Y, delta.X * boundAction.Z, 0.0f);
                }
                catch (Exception e)
                {
                    Logger.Error($"Error in OnInput: {e.Message}", e);
                }
            }
        });
        
        triggeredActions = CurrentContext.Match(MouseAxis.WheelY, modifiers);
        triggeredActions.ForEach(triggeredActionId =>
        {
            OnInputEvent.TryGetValue(triggeredActionId, out var inputActionList);
            if (inputActionList == null) return;
                
            foreach (var boundAction in inputActionList)
            {
                try 
                {
                    boundAction.Action.Invoke(delta.Y * boundAction.X, delta.Y * boundAction.Y, delta.Y * boundAction.Z, 0.0f);
                }
                catch (Exception e)
                {
                    Logger.Error($"Error in OnInput: {e.Message}", e);
                }
            }
        });
    }
    
    private void OnMouseButtonDown(IMouse mouse, MouseButton button)
    {
        _heldMouseButtons.Add(button);
        
        var triggeredActions = CurrentContext.Match(button, GetModifiers());
        triggeredActions.ForEach(triggeredActionId =>
        {
            OnInputEvent.TryGetValue(triggeredActionId, out var inputActionList);
            if (inputActionList == null) return;
                
            foreach (var boundAction in inputActionList)
            {
                try 
                {
                    boundAction.Action.Invoke(boundAction.X, boundAction.Y, boundAction.Z, 0.0f);
                }
                catch (Exception e)
                {
                    Logger.Error($"Error in OnInput: {e.Message}", e);
                }
            }
        });
    }
    
    private void OnMouseButtonUp(IMouse mouse, MouseButton button)
    {
        _heldMouseButtons.Remove(button);
        
        var triggeredActions = CurrentContext.Match(button, GetModifiers());
        triggeredActions.ForEach(triggeredActionId =>
        {
            OnInputReleasedEvent.TryGetValue(triggeredActionId, out var inputActionList);
            if (inputActionList == null) return;
                
            foreach (var boundAction in inputActionList)
            {
                try 
                {
                    boundAction.Action.Invoke(boundAction.X, boundAction.Y, boundAction.Z, 0.0f);
                }
                catch (Exception e)
                {
                    Logger.Error($"Error in OnInputReleased: {e.Message}", e);
                }
            }
        });
    }

    /// <summary>
    /// Process OnMouseButtonHeld and OnInputHeld events for all currently held mouse buttons.
    /// </summary>
    /// <returns>List of triggered input actions (by long representation of the bound enum)</returns>
    private void SendHeldMouseButtonEvents(List<KeyModifiers> modifiers, ref Dictionary<string, List<BoundHeldAction>> triggeredHandlers)
    {
        foreach (var heldButton in _heldMouseButtons)
        {
            var triggeredActions = CurrentContext.Match(heldButton, modifiers);
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