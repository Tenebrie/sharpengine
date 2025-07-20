using Engine.Core.Common;
using Engine.Core.Extensions;
using Engine.Worlds.Attributes;
using Engine.Worlds.Entities;
using Engine.Worlds.Services;
using Silk.NET.Input;
using User.Game.Actors;
using User.Game.Player.Components;
using User.Game.Services;

namespace User.Game.Player;

public class PlayerCharacter : Actor
{
    private const double MovementSpeed = 150.0;
    private const double RotationSpeed = 0.12;
    
    private double _pitch = 20.0;
    private double _yaw = 0.0;
    
    [Component] public MainCamera MainCameraComponent;
    [Component] public DragonMesh DragonMeshComponent;

    [OnInit]
    protected void OnInit()
    {
        _savedMousePosition = GetService<InputService>().GetMousePosition();
        OnCameraRotate(Vector2.Zero);
    }
    
    [OnInputHeld(InputAction.MoveForward,  +1.0, +0.0)]
    [OnInputHeld(InputAction.MoveBackward, -1.0, -0.0)]
    [OnInputHeld(InputAction.MoveLeft,     +0.0, -1.0)]
    [OnInputHeld(InputAction.MoveRight,    +0.0, +1.0)]
    protected void OnMove(double deltaTime, Vector2 direction)
    {
        var value = new Vector(direction.Y, 0, -direction.X).Normalized();
        Transform.TranslateLocal(value * MovementSpeed * deltaTime);
    }
    
    [OnInput(InputAction.CameraRotatePitch, +1.0, +0.0)]
    [OnInput(InputAction.CameraRotateYaw,   +0.0, +1.0)]
    protected void OnCameraRotate(Vector2 direction)
    {
        _pitch += direction.X * RotationSpeed;
        _yaw += direction.Y * RotationSpeed;
        Transform.Rotation = Transform.FromRotation(0, _yaw, 0).Rotation;
        MainCameraComponent.Transform.Position = new Vector(0, 3, 0);
        MainCameraComponent.Camera.Transform = Transform.FromRotation(_pitch, 0, 0).TranslateLocal(new Vector(0, 3, 17));
        GetService<InputService>().SetMousePosition(_savedMousePosition);
    }
    
    private Vector2 _savedMousePosition = Vector2.Zero;

    [OnInput(InputAction.HoldToControlCamera)]
    protected void OnStartMovingCamera()
    {
        var inputService = GetService<InputService>();
        
        _savedMousePosition = inputService.GetMousePosition();
        inputService.SetMouseCursorMode(CursorMode.Hidden);
    }
    
    [OnInputReleased(InputAction.HoldToControlCamera)]
    protected void OnStopMovingCamera()
    {
        var inputService = GetService<InputService>();
        
        inputService.SetMouseCursorMode(CursorMode.Normal);
        inputService.SetMousePosition(_savedMousePosition);
    }
    
}