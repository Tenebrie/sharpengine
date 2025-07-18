using Engine.Core.Common;
using Engine.Input.Attributes;
using Engine.Input.Contexts;
using Engine.Worlds.Attributes;
using Engine.Worlds.Entities;
using Engine.Worlds.Entities.BuiltIns;
using Silk.NET.Input;
using InputService = Engine.Worlds.Services.InputService;

namespace User.Game.Actors;

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

public class MainCamera : Camera 
{
    private const double MovementSpeed = 5.0;
    private const double TiltSpeed = 1.0;

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
    
    [OnInputHeld(InputAction.CameraForward,  +0.0, +1.0)]
    [OnInputHeld(InputAction.CameraBackward, +0.0, -1.0)]
    [OnInputHeld(InputAction.CameraLeft,     -1.0, +0.0)]
    [OnInputHeld(InputAction.CameraRight,    +1.0, +0.0)]
    protected void OnCameraPan(double deltaTime, Vector2 direction)
    {
        var value = new Vector(direction.X, direction.Y, 0).Normalized();
        Transform.Translate(value * MovementSpeed * deltaTime);
    }

    [OnInputHeld(InputAction.CameraTiltForward,  +0.0, +1.0)]
    [OnInputHeld(InputAction.CameraTiltBackward, +0.0, -1.0)]
    [OnInputHeld(InputAction.CameraTiltLeft,     -1.0, +0.0)]
    [OnInputHeld(InputAction.CameraTiltRight,    +1.0, +0.0)]
    protected void OnCameraTilt(double deltaTime, Vector2 direction) 
    {
        Transform.Rotate(direction.Y * TiltSpeed, 0.0, direction.X * TiltSpeed);
    }
}
