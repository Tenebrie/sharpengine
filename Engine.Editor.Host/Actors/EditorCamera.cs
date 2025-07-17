using Engine.Core.Common;
using Engine.Editor.Host.Services;
using Engine.Worlds.Attributes;
using Engine.Worlds.Entities;

namespace Engine.Editor.Host.Actors;

public class EditorCamera : Camera
{
    private const double MovementSpeed = 7.0;
    private const double TiltSpeed = 90.0;

    [OnInit]
    protected void OnInit()
    {
        ActiveInEditor = true; 
    }

    [OnInputHeld(InputAction.CameraForward,  +0.0, +1.0)]
    [OnInputHeld(InputAction.CameraBackward, +0.0, -1.0)]
    [OnInputHeld(InputAction.CameraLeft,     -1.0, +0.0)]
    [OnInputHeld(InputAction.CameraRight,    +1.0, +0.0)]
    protected void OnCameraPan(double deltaTime, Vector2 direction)
    {
        var value = new Vector(direction.X, direction.Y, 0).NormalizedCopy();
        Transform.Translate(value * MovementSpeed * deltaTime);
    }

    [OnInputHeld(InputAction.CameraTiltForward,  +0.0, +1.0)]
    [OnInputHeld(InputAction.CameraTiltBackward, +0.0, -1.0)]
    [OnInputHeld(InputAction.CameraTiltLeft,     -1.0, +0.0)]
    [OnInputHeld(InputAction.CameraTiltRight,    +1.0, +0.0)]
    protected void OnCameraTilt(double deltaTime, Vector2 direction) 
    {
        Transform.Rotate(direction.Y * TiltSpeed * deltaTime, 0.0, direction.X * TiltSpeed * deltaTime);
    }
}
