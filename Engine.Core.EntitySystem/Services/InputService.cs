using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Input;
using Engine.Core.Input.Contexts;
using Silk.NET.Input;

namespace Engine.Core.EntitySystem.Services;

public partial class InputService : Service
{
    private readonly InputHandler _inputHandler = new();
    
    public InputContext InputContext
    {
        get => _inputHandler.CurrentContext;
        set => _inputHandler.CurrentContext = value;
    }
    
    public bool IsInputHeld(object inputAction)
    {
        if (inputAction is long longValue)
            return _inputHandler.IsInputHeld(longValue);
        
        if (inputAction.GetType().IsEnum)
            return _inputHandler.IsInputHeld(Convert.ToInt64(inputAction));
        
        throw new ArgumentException($"Input action must be a long or an enum type, but was {inputAction.GetType().Name}");
    }
    public bool IsKeyHeld(Key key) => _inputHandler.IsKeyHeld(key);
    public bool IsMouseButtonHeld(MouseButton button) => _inputHandler.IsMouseButtonHeld(button);
    public void BindMouseEvents(IMouse mouse) => _inputHandler.BindMouseEvents(mouse);
    public void BindKeyboardEvents(IKeyboard keyboard) => _inputHandler.BindKeyboardEvents(keyboard);
    public void SendKeyboardHeldEvents(double deltaTime) => _inputHandler.SendHeldInputEvents(deltaTime);
    public void ClearSubscriptions(Atom owner) => _inputHandler.ClearSubscriptions(owner);
    
    public Vector2 GetMousePosition() => _inputHandler.GetMousePosition();
    public void SetMousePosition(Vector2 position) => _inputHandler.SetMousePosition(position);
    public void SetMouseCursor(StandardCursor cursor) => _inputHandler.SetMouseCursor(cursor);
    public void SetMouseCursorMode(CursorMode mode) => _inputHandler.SetMouseCursorMode(mode);

    [OnDestroy]
    protected void OnDestroy()
    {
        _inputHandler.UnbindMouseEvents();
        _inputHandler.UnbindKeyboardEvents();
    }
    
    public Dictionary<long, List<BoundHeldAction>> OnInputEvent => _inputHandler.OnInputEvent;
    public Dictionary<long, List<BoundHeldAction>> OnInputHeldEvent => _inputHandler.OnInputHeldEvent;
    public Dictionary<long, List<BoundHeldAction>> OnInputReleasedEvent => _inputHandler.OnInputReleasedEvent;
    public Dictionary<Key, List<BoundHeldAction>> OnKeyboardKeyEvent => _inputHandler.OnKeyboardKeyEvent;
    public Dictionary<Key, List<BoundHeldAction>> OnKeyboardKeyHeldEvent => _inputHandler.OnKeyboardKeyHeldEvent;
    public Dictionary<Key, List<BoundHeldAction>> OnKeyboardKeyReleasedEvent => _inputHandler.OnKeyboardKeyReleasedEvent;
}