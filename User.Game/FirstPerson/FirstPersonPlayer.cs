using Engine.Core.Common;
using Engine.Core.Communication.Groups;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Physics;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Services;
using Engine.Core.Input;
using Engine.Core.Input.Attributes;
using Silk.NET.Input;
using User.Game.Actors;
using User.Game.Player.PlayerAttributes;
using User.Game.Services;

namespace User.Game.FirstPerson;

public partial class FirstPersonPlayer : Actor
{
    [DefaultGroup] public static readonly Group<FirstPersonPlayer> All = new();
    [Component] public MainCamera MainCameraComponent;
    [Component] public PhysicsComponent Physics;
    [Component] public FirstPersonAbilityController AbilityController;
    [Component] public PlayerAttributesComponent Attributes;
    // [Component] public FirstPersonWeapon Weapon;
    
    private const double RotationSpeed = 200;
    private double _pitch = 0.0;
    private double _yaw = 0.0;

    [OnInput(InputAction.MoveForward,  +1.0, +0.0)]
    [OnInput(InputAction.MoveBackward, -1.0, -0.0)]
    [OnInput(InputAction.MoveLeft,     +0.0, -1.0)]
    [OnInput(InputAction.MoveRight,    +0.0, +1.0)]
    protected void OnMoveInput(Vector2 direction)
    {
    }

    [OnInput(InputAction.CameraLookX, 1.0, 0.0)]
    [OnInput(InputAction.CameraLookY, 0.0, 1.0)]
    protected void OnCameraLook(Vector2 direction)
    {
        if (!Backstage.Window.HasFocus)
        {
            GetService<InputService>().SetMouseCursorMode(CursorMode.Normal);
            return;
        }
        var screenSize = Backstage.Window.Size;
        _pitch += direction.Y / screenSize.X * RotationSpeed;
        _yaw += direction.X / screenSize.X * RotationSpeed;
        _pitch = Math.Clamp(_pitch, -89.9, 89.9);
        Transform.Rotation = Transform.Identity
            .RotateAroundLocal(Vector3.Right, _pitch)
            .RotateAroundGlobal(Vector3.Up, _yaw)
            .Rotation;
        GetService<InputService>().SetMousePosition(Backstage.Window.Size / 2);
        GetService<InputService>().SetMouseCursorMode(CursorMode.Hidden);
    }
    
    [OnInputHeld(InputAction.CameraLookX, 1.0, 0.0)]
    [OnInputHeld(InputAction.CameraLookY, 0.0, 1.0)]
    protected void OnCameraLookGamepad(double deltaTime, Vector2 direction)
    {
        if (direction.LengthSquared == 0)
            return;
        
        _pitch += direction.Y * RotationSpeed * deltaTime * 0.5;
        _yaw += direction.X * RotationSpeed * deltaTime * 0.5;
        _pitch = Math.Clamp(_pitch, -89.9, 89.9);
        Transform.Rotation = Transform.Identity
            .RotateAroundLocal(Vector3.Right, -_pitch)
            .RotateAroundGlobal(Vector3.Up, _yaw)
            .Rotation;
    }
    
    [OnInput(InputAction.Jump)]
    protected void OnJumpInput()
    {
        if (Transform.Position.Y <= 0.1)
        {
            Physics.LinearVelocity += Vector3.Up * 6.0;
        }
    }
    
    private bool _wasFallingLastFrame = false;
    
    [OnUpdate]
    protected void OnUpdate(double deltaTime)
    {
        var inputService = GetService<InputService>();
        
        var directionMovement = Vector3.Zero;
        if (inputService.IsInputHeld(InputAction.MoveForward))
            directionMovement += Vector3.Forward;
        if (inputService.IsInputHeld(InputAction.MoveBackward))
            directionMovement += Vector3.Backward;
        if (inputService.IsInputHeld(InputAction.MoveLeft))
            directionMovement += Vector3.Left;
        if (inputService.IsInputHeld(InputAction.MoveRight))
            directionMovement += Vector3.Right;
        
        var keyboardMovementAmount = directionMovement.Length > 0 ? 1.0 : 0.0;
        
        var stickInput = new Vector3(inputService.GetGamepadAnalogPosition(GamepadAnalog.LeftThumbstickX).X,
            -inputService.GetGamepadAnalogPosition(GamepadAnalog.LeftThumbstickY).Y, 0);
        var stickInputAmount = stickInput.Length;
        directionMovement += Vector3.Forward * -inputService.GetGamepadAnalogPosition(GamepadAnalog.LeftThumbstickY).Y;
        directionMovement += Vector3.Right * inputService.GetGamepadAnalogPosition(GamepadAnalog.LeftThumbstickX).X;

        directionMovement = WorldTransform.Basis.TransformVector(directionMovement.Normalized());
        directionMovement.Y = 0;
        directionMovement = directionMovement.Normalized() * Math.Min(1, stickInputAmount + keyboardMovementAmount);
        
        if (Transform.Position.Y <= 0.0)
            Transform.Position = new Vector3(Transform.Position.X, 0.0, Transform.Position.Z);

        const double movementSpeed = 16.0;
        const double baseAcceleration = 10.0;
        const double baseDeceleration = 20.0;
        const double airControlFactor = 0.2;
        
        var isFalling = Transform.Position.Y > 0.0;
        
        var acceleration = baseAcceleration;
        if (isFalling)
            acceleration *= airControlFactor;
        
        var verticalVelocity = Physics.LinearVelocity.Y;
        var targetDirection = directionMovement * movementSpeed;
        
        if (directionMovement.LengthSquared == 0 && !isFalling)
        {
            acceleration = baseDeceleration;
        }
        
        Physics.LinearVelocity += (targetDirection - Physics.LinearVelocity) * acceleration * deltaTime;
        Physics.GravityEnabled = Transform.Position.Y > 0.0;
        if (isFalling)
            Physics.LinearVelocity.Y = verticalVelocity;
        else if (Physics.LinearVelocity.Y < 0.0)
            Physics.LinearVelocity.Y = 0.0;

        switch (isFalling)
        {
            case false when _wasFallingLastFrame:
                MainCameraComponent.TargetOffset += Vector3.Down * 1.5;
                break;
            case true when !_wasFallingLastFrame:
                MainCameraComponent.TargetOffset += Vector3.Down;
                break;
        }
        
        _wasFallingLastFrame = isFalling;
    }
}