using Engine.Core.Common;
using Engine.Editor.Host.Services;
using Engine.Worlds.Attributes;
using Engine.Worlds.Entities;

namespace Engine.Editor.Host.Actors;

public class EditorCamera : Camera
{
    private const double MovementSpeed = 7.0;
    private const double RotationSpeed = 0.12;
    
    private double _pitch = 0.0;
    private double _yaw = 0.0;

    [OnInit]
    protected void OnInit()
    {
        ActiveInEditor = true; 
    }
    
    [OnInputHeld(InputAction.CameraUp,       +0.0, +0.0, +1.0)]
    [OnInputHeld(InputAction.CameraDown,     +0.0, +0.0, -1.0)]
    [OnInputHeld(InputAction.CameraLeft,     +0.0, -1.0, +0.0)]
    [OnInputHeld(InputAction.CameraRight,    +0.0, +1.0, +0.0)]
    [OnInputHeld(InputAction.CameraForward,  +1.0, +0.0, +0.0)]
    [OnInputHeld(InputAction.CameraBackward, -1.0, +0.0, +0.0)]
    protected void OnCameraPan(double deltaTime, Vector direction)
    {
        var value = (Transform.Basis.Forward * direction.X + Transform.Basis.Right * direction.Y + Transform.Basis.Up * direction.Z) * MovementSpeed * deltaTime;
        value.Normalize();
        Transform.Translate(value * MovementSpeed * deltaTime);
    }

    [OnInput(InputAction.CameraRotatePitch, +1.0, +0.0)]
    [OnInput(InputAction.CameraRotateYaw,   +0.0, +1.0)]
    protected void OnCameraRotate(Vector2 direction)
    {
        Console.WriteLine(direction);
        _pitch += direction.X * RotationSpeed;
        _yaw += direction.Y * RotationSpeed;
        Transform.Rotation = Transform.Identity
            .RotateAroundGlobal(Vector.UnitY, _yaw)
            .RotateAroundLocal(Vector.UnitX, _pitch)
            .Rotation;
    }
}
