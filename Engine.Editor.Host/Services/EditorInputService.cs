using Engine.Input.Attributes;
using Engine.Input.Contexts;
using Engine.Worlds.Attributes;
using Engine.Worlds.Entities;
using Engine.Worlds.Services;
using Silk.NET.Input;

namespace Engine.Editor.Host.Services;

[InputActions]
public enum InputAction
{
    CameraForward,
    CameraBackward,
    CameraLeft,
    CameraRight,
    CameraTiltForward,
    CameraTiltBackward,
    CameraTiltLeft,
    CameraTiltRight,
}

public class EditorInputService : Service
{
    [OnInit]
    protected void OnInit()
    {
        var defaultContext = InputContext.GetBuilder<InputAction>()
            .Add(InputAction.CameraForward, Key.W)
            .Add(InputAction.CameraForward, Key.Up)
            .Add(InputAction.CameraBackward, Key.S)
            .Add(InputAction.CameraBackward, Key.Down)
            .Add(InputAction.CameraLeft, Key.A)
            .Add(InputAction.CameraLeft, Key.Left)
            .Add(InputAction.CameraRight, Key.D)
            .Add(InputAction.CameraRight, Key.Right)
            .Add(InputAction.CameraTiltForward, Key.Keypad8)
            .Add(InputAction.CameraTiltBackward, Key.Keypad2)
            .Add(InputAction.CameraTiltLeft, Key.Keypad4)
            .Add(InputAction.CameraTiltRight, Key.Keypad6)
            .Build();
        
        GetService<InputService>().InputContext = defaultContext;
    }

    [OnKeyInput(Key.F3)]
    protected void OnToggleRendererDebug()
    {
        
    }
}