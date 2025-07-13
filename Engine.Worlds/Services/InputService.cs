using Engine.Input;
using Engine.Input.Contexts;
using Engine.Worlds.Attributes;
using Engine.Worlds.Entities;
using Silk.NET.Input;

namespace Engine.Worlds.Services;

public class InputService : Service
{
    private readonly Input.InputService _inputService = new();
    
    public InputContext InputContext
    {
        get => _inputService.CurrentContext;
        set => _inputService.CurrentContext = value;
    }
    
    public void BindMouseEvents(IMouse mouse) => _inputService.BindMouseEvents(mouse);
    public void BindKeyboardEvents(IKeyboard keyboard) => _inputService.BindKeyboardEvents(keyboard);
    public void SendKeyboardHeldEvents(double deltaTime) => _inputService.SendKeyboardHeldEvents(deltaTime);
}