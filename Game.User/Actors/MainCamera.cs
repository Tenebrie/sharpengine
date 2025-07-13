using Engine.Core.Common;
using Engine.Input;
using Engine.Input.Attributes;
using Engine.Input.Contexts;
using Engine.Worlds.Attributes;
using Engine.Worlds.Entities;
using Engine.Worlds.Entities.BuiltIns;
using Silk.NET.Input;

namespace Game.User.Actors;

[InputActions]
public enum InputAction
{
    Jump,
    CameraForward,
    CameraBackward,
    CameraLeft,
    CameraRight
}

public class MainCamera : Camera
{
    private const double MovementSpeed = 3.0;
    
    [OnInit]
    protected void OnInit()
    {
        var defaultContext = InputContext.GetBuilder<InputAction>()
            .Add(InputAction.Jump,           Key.Space)
            .Add(InputAction.CameraForward,  Key.W)
            .Add(InputAction.CameraForward,  Key.Up)
            .Add(InputAction.CameraBackward, Key.S)
            .Add(InputAction.CameraBackward, Key.Down)
            .Add(InputAction.CameraLeft,     Key.A)
            .Add(InputAction.CameraLeft,     Key.Left)
            .Add(InputAction.CameraRight,    Key.D)
            .Add(InputAction.CameraRight,    Key.Right)
            .Build();
        InputHandler.SetInputContext(defaultContext);
    }
    
    [OnInput(InputAction.Jump)]
    protected void OnJump()
    {
        Console.WriteLine("Jump action triggered!");
    }
    
    [OnInput(Key.J)]
    protected void OnHardcodedJump()
    {
        Console.WriteLine("Hardcoded jump action triggered!");
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
