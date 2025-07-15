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
}

public class MainCamera : Camera
{
    private const double MovementSpeed = 5.0;
    
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
            .Build();
        
        GetService<InputService>().InputContext = defaultContext;
    }
    
    [OnInputHeld(InputAction.CameraForward,  +0.0, +1.0)]
    [OnInputHeld(InputAction.CameraBackward, +0.0, -1.0)]
    [OnInputHeld(InputAction.CameraLeft,     -1.0, +0.0)]
    [OnInputHeld(InputAction.CameraRight,    +1.0, +0.0)]
    protected void LiterallyAnyFunction(double deltaTime, Vector2 direction)
    {
        var value = new Vector(direction.X, direction.Y, 0).NormalizedCopy();
        Transform.Translate(value * MovementSpeed * deltaTime);
    }
}
