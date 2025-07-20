using Engine.Core.Common;
using Engine.Core.Extensions;
using Engine.Editor.Host.Services;
using Engine.Worlds.Attributes;
using Engine.Worlds.Entities;
using Engine.Worlds.Services;
using Silk.NET.Input;
using Axis = Engine.Core.Common.Axis;

namespace Engine.Editor.Host.Actors;

public class EditorCamera : Camera
{
    private int _movementSpeedIncrement = 10;
    private double _movementSpeed = 50.0;
    private const double RotationSpeed = 0.12;
    
    private double _pitch = 0.0;
    private double _yaw = 0.0;

    [OnInit]
    protected void OnInit()
    {
        IsEditorCamera = true; 
    }
    
    [OnInputHeld(InputAction.CameraUp,       +0.0, +0.0, +1.0)]
    [OnInputHeld(InputAction.CameraDown,     +0.0, +0.0, -1.0)]
    [OnInputHeld(InputAction.CameraLeft,     +0.0, -1.0, +0.0)]
    [OnInputHeld(InputAction.CameraRight,    +0.0, +1.0, +0.0)]
    [OnInputHeld(InputAction.CameraForward,  +1.0, +0.0, +0.0)]
    [OnInputHeld(InputAction.CameraBackward, -1.0, +0.0, +0.0)]
    protected void OnCameraPan(double deltaTime, Vector direction)
    {
        var value = (Transform.Basis.Forward * direction.X + Transform.Basis.Right * direction.Y + Transform.Basis.Up * direction.Z) * _movementSpeed * deltaTime;
        value.Normalize();
        Transform.TranslateGlobal(value * _movementSpeed * deltaTime);
    }

    [OnInput(InputAction.CameraRotatePitch, +1.0, +0.0)]
    [OnInput(InputAction.CameraRotateYaw,   +0.0, +1.0)]
    protected void OnCameraRotate(Vector2 direction)
    {
        _pitch += direction.X * RotationSpeed;
        _yaw += direction.Y * RotationSpeed;
        Transform.Rotation = Transform.Identity
            .RotateAroundGlobal(Axis.Yaw, _yaw)
            .RotateAroundLocal(Axis.Pitch, _pitch)
            .Rotation;
        GetService<InputService>().SetMousePosition(_savedMousePosition);
    }
    
    [OnInput(InputAction.CameraSpeedWheel, 1.0)]
    protected void OnChangeCameraSpeed(double value)
    {
        _movementSpeedIncrement += (int)Math.Round(value);
        _movementSpeed = Math.Pow(2, _movementSpeedIncrement / 8.0) * 50.0;
        _movementSpeed = Math.Clamp(_movementSpeed, 1.0, 3000.0);
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
