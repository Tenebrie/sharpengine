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
    
    public void BindMouseEvents(IMouse mouse) => _inputHandler.BindMouseEvents(mouse);
    public void BindKeyboardEvents(IKeyboard keyboard) => _inputHandler.BindKeyboardEvents(keyboard);
    public void SendKeyboardHeldEvents(double deltaTime) => _inputHandler.SendKeyboardHeldEvents(deltaTime);
    public void ClearSubscriptions(Atom owner) => _inputHandler.ClearSubscriptions(owner);

    [OnDestroy]
    protected void OnDestroy()
    {
        _inputHandler.ClearKeyboardEvents();
    }
    
    public Dictionary<long, List<BoundHeldAction>> OnInputEvent => _inputHandler.OnInputEvent;
    public Dictionary<long, List<BoundHeldAction>> OnInputHeldEvent => _inputHandler.OnInputHeldEvent;
    public Dictionary<long, List<BoundHeldAction>> OnInputReleasedEvent => _inputHandler.OnInputReleasedEvent;
    public Dictionary<Key, List<BoundHeldAction>> OnKeyboardKeyEvent => _inputHandler.OnKeyboardKeyEvent;
    public Dictionary<Key, List<BoundHeldAction>> OnKeyboardKeyHeldEvent => _inputHandler.OnKeyboardKeyHeldEvent;
    public Dictionary<Key, List<BoundHeldAction>> OnKeyboardKeyReleasedEvent => _inputHandler.OnKeyboardKeyReleasedEvent;
}