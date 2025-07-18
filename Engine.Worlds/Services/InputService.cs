using Engine.Input;
using Engine.Input.Contexts;
using Engine.Worlds.Attributes;
using Engine.Worlds.Entities;
using Silk.NET.Input;

namespace Engine.Worlds.Services;

public class InputService : Service
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