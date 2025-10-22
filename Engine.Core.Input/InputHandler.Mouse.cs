using Engine.Core.Common;
using Engine.Core.Logging;
using Engine.Core.Modules.EntitySystem;
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
        _lastMousePosition = (Math.Floor(position.X), Math.Floor(position.Y));
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

    private void UnbindMouseEvents()
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
        UserInputMode = UserInputMode.KeyboardAndMouse;
        
        var deltaX = position.X - _lastMousePosition.X;
        var deltaY = position.Y - _lastMousePosition.Y;
        if (deltaX == 0 && deltaY == 0)
            return;
        _lastMousePosition = new Vector2(position.X, position.Y);
        var modifiers = GetModifiers();
        var triggeredActions = CurrentContext.Match(MouseAxis.MoveX, modifiers);
        TriggerOnInputEngaged(triggeredActions, (deltaX, deltaX, deltaX));
        
        triggeredActions = CurrentContext.Match(MouseAxis.MoveY, modifiers);
        TriggerOnInputEngaged(triggeredActions, (deltaY, deltaY, deltaY));
    }
    
    private void OnMouseScroll(IMouse mouse, ScrollWheel delta)
    {
        var modifiers = GetModifiers();
        var triggeredActions = CurrentContext.Match(MouseAxis.WheelX, modifiers);
        TriggerOnInputEngaged(triggeredActions, (delta.X, delta.X, delta.X));
        
        triggeredActions = CurrentContext.Match(MouseAxis.WheelY, modifiers);
        TriggerOnInputEngaged(triggeredActions, (delta.Y, delta.Y, delta.Y));
    }
    
    private void OnMouseButtonDown(IMouse mouse, MouseButton button)
    {
        _heldMouseButtons.Add(button);
        
        var triggeredActions = CurrentContext.Match(button, GetModifiers());
        TriggerOnInputEngaged(triggeredActions, Vector3.One);
    }
    
    private void OnMouseButtonUp(IMouse mouse, MouseButton button)
    {
        _heldMouseButtons.Remove(button);
        
        var triggeredActions = CurrentContext.Match(button, GetModifiers());
        TriggerOnInputReleased(triggeredActions, Vector3.One);
    }

    /// <summary>
    /// Process OnMouseButtonHeld and OnInputHeld events for all currently held mouse buttons.
    /// </summary>
    /// <returns>List of triggered input actions (by long representation of the bound enum)</returns>
    private void SendHeldMouseButtonEvents(IBackstage identity, List<KeyModifiers> modifiers, ref Dictionary<string, List<BoundHeldAction>> triggeredHandlers)
    {
        foreach (var heldButton in _heldMouseButtons)
        {
            var triggeredActions = CurrentContext.Match(heldButton, modifiers);
            TriggerOnInputHeld(identity, triggeredActions, Vector3.One, ref triggeredHandlers);
        }
    }
}