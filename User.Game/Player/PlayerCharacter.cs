using Engine.Core.Makers;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Services;
using Engine.Core.Extensions;
using Silk.NET.Input;
using User.Game.Player.Abilities;
using User.Game.Player.Components;
using User.Game.Services;
using Vector2 = Engine.Core.Common.Vector2;
using Vector3 = Engine.Core.Common.Vector3;

namespace User.Game.Player;

public partial class PlayerCharacter : Actor
{
    private Quaternion _desiredRotation = Quaternion.Identity;
    private const double MovementSpeed = 50.0;
    private const double RotationSpeed = 6;
    
    private Vector3 _velocity = Vector3.Zero;
    private Vector3 _acceleration = Vector3.Zero;
    private Quaternion _currentRotation = Quaternion.Identity;
    private double _currentRoll = 0.0;
    private bool _inputHeldThisFrame = false;

    [Component] public AbilityController Abilities;
    [Component] public DragonMesh DragonMeshComponent;
    // [Component] public PhysicsComponent PhysicsComponent;
    [Component] public ExperienceComponent Experience;

    [OnInputHeld(InputAction.MoveForward,  +1.0, +0.0)]
    [OnInputHeld(InputAction.MoveBackward, -1.0, -0.0)]
    [OnInputHeld(InputAction.MoveLeft,     +0.0, -1.0)]
    [OnInputHeld(InputAction.MoveRight,    +0.0, +1.0)]
    protected void OnMove(double deltaTime, Vector2 direction)
    {
        if (direction.LengthSquared == 0)
            return;
        
        var value = new Vector3(direction.Y, 0, -direction.X).Normalized();
        // Transform.TranslateGlobal(value * MovementSpeed * deltaTime);
        // PhysicsComponent.LinearVelocity = value * MovementSpeed * 2;
        _acceleration = value * 15 * deltaTime;
        
        var forwardVector = Vector3.Forward;
        var dotProduct = value.DotProduct(forwardVector);
        var crossProduct = value.CrossProduct(forwardVector);
        var difference = Math.Atan2(crossProduct.Y, dotProduct);
        var targetRotation = QuatMakers.FromRotationRadians(0, difference, 0);
        
        var angle = _currentRotation.SignedAngleTo(targetRotation, Vector3.Up);
        
        _desiredRotation = QuatMakers.FromRotationRadians(0, difference, 0);
        var rotationSpeed = Math.Min(RotationSpeed * deltaTime, 1.0);
        _currentRotation = Quaternion.Slerp(_currentRotation, _desiredRotation, rotationSpeed);

        var targetRoll = -angle;
        _currentRoll = Math.Clamp(
            _currentRoll + (targetRoll - _currentRoll) * rotationSpeed, 
            -30,
            30
        );
        Transform.Rotation = _currentRotation;
        Transform.RotateAroundLocal(Vector3.Forward, _currentRoll);
        _inputHeldThisFrame = true;
    }

    [OnUpdate]
    protected void OnUpdate(double deltaTime)
    {
        if (!_inputHeldThisFrame)
            _acceleration = Vector3.Zero;
        _velocity += _acceleration;

        var maxSpeed = 1.5;
        if (GetService<InputService>().IsKeyHeld(Key.ShiftLeft))
        {
            maxSpeed = 4.5;
        }
        
        if (_velocity.Length > maxSpeed)
            _velocity = _velocity.SetLengthIfNotZero(maxSpeed);
        Transform.TranslateGlobal(_velocity * MovementSpeed * deltaTime);
        // PhysicsComponent.LinearVelocity = _velocity * MovementSpeed;
        if (!_inputHeldThisFrame)
            _velocity -= _velocity * 2.0 * deltaTime;
        
        var wasInputHeldThisFrame = _inputHeldThisFrame;
        _inputHeldThisFrame = false;
        if (wasInputHeldThisFrame)
            return;
     
        var rotationSpeed = Math.Min(RotationSpeed * deltaTime, 1.0);
        _currentRoll = Math.Clamp(
            _currentRoll + (0 - _currentRoll) * rotationSpeed, 
            -25,
            25
        );
        Transform.Rotation = _currentRotation;
        Transform.RotateAroundLocal(Vector3.Forward, _currentRoll);

        _inputHeldThisFrame = true;
    }
    
    [OnInput(InputAction.Jump)]
    protected void OnJump()
    {
        // if (Transform.Position.Y > 0)
            // return;
        // PhysicsComponent.LinearVelocity.Y = 20.0;
    }
}
