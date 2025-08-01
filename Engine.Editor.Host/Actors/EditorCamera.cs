using Engine.Core.Common;
using Engine.Core.Logging;
using Engine.Editor.Host.Services;
using Engine.Worlds.Attributes;
using Engine.Worlds.Entities;
using Engine.Worlds.Services;
using Silk.NET.Input;

namespace Engine.Editor.Host.Actors;

public partial class EditorCamera : Camera
{
    private int _movementSpeedIncrement = 10;
    private double _movementSpeed = 50.0;
    private const double RotationSpeed = 250;
    
    private double _pitch = 0.0;
    private double _yaw = 0.0;

    [OnInit]
    protected void OnInit()
    {
        IsEditorCamera = true; 
        Transform.Position = new Vector3(0.0, 50.0, 50.0);
        _pitch = 45;
        Transform.Rotation = Transform.Identity
            .RotateAroundGlobal(Vector3.Yaw, _yaw)
            .RotateAroundLocal(Vector3.Pitch, _pitch)
            .Rotation;
    }
    
    [OnInputHeld(InputAction.CameraUp,       +0.0, +0.0, +1.0)]
    [OnInputHeld(InputAction.CameraDown,     +0.0, +0.0, -1.0)]
    [OnInputHeld(InputAction.CameraLeft,     +0.0, -1.0, +0.0)]
    [OnInputHeld(InputAction.CameraRight,    +0.0, +1.0, +0.0)]
    [OnInputHeld(InputAction.CameraForward,  +1.0, +0.0, +0.0)]
    [OnInputHeld(InputAction.CameraBackward, -1.0, +0.0, +0.0)]
    protected void OnCameraPan(double deltaTime, Vector3 direction)
    {
        var value = (Transform.Basis.Forward * direction.X + Transform.Basis.Right * direction.Y + Transform.Basis.Up * direction.Z);
        value.NormalizeInPlace();
        Transform.TranslateGlobal(value * _movementSpeed * deltaTime);
    }

    [OnInput(InputAction.CameraRotatePitch, +1.0, +0.0)]
    [OnInput(InputAction.CameraRotateYaw,   +0.0, +1.0)]
    protected void OnCameraRotate(Vector2 direction)
    {
        var screenSize = Backstage.GetWindow().Size;
        _pitch += direction.X / screenSize.X * RotationSpeed;
        _yaw += direction.Y / screenSize.X * RotationSpeed;
        Transform.Rotation = Transform.Identity
            .RotateAroundGlobal(Vector3.Yaw, _yaw)
            .RotateAroundLocal(Vector3.Pitch, _pitch)
            .Rotation;
        GetService<InputService>().SetMousePosition(_savedMousePosition);
    }

    [OnInit]
    protected void SetInitialCamera() => OnChangeCameraSpeed(0.0);
    
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
