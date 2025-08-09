using Engine.Core.Common;
using Engine.Core.Makers;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Physics;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Services;
using User.Game.Actors;
using User.Game.Player.Abilities;
using User.Game.Player.Components;
using User.Game.Services;
using Vector3 = Engine.Core.Common.Vector3;

namespace User.Game.Player;

public partial class PlayerCharacter : Actor
{
    private const double MovementSpeed = 50.0;
    private const double RotationSpeed = 0.12;

    [Component] public AbilityController Abilities;
    [Component] public DragonMesh DragonMeshComponent;
    [Component] public PhysicsComponent PhysicsComponent;

    [OnInputHeld(InputAction.MoveForward,  +1.0, +0.0)]
    [OnInputHeld(InputAction.MoveBackward, -1.0, -0.0)]
    [OnInputHeld(InputAction.MoveLeft,     +0.0, -1.0)]
    [OnInputHeld(InputAction.MoveRight,    +0.0, +1.0)]
    protected void OnMove(double deltaTime, Vector2 direction)
    {
        if (direction.LengthSquared == 0)
            return;
        
        var value = new Vector3(direction.Y, 0, -direction.X).Normalized();
        var forwardVector = Vector3.Forward;
        var dotProduct = value.DotProduct(forwardVector);
        var crossProduct = value.CrossProduct(forwardVector);
        var difference = Math.Atan2(crossProduct.Y, dotProduct);
        Transform.TranslateGlobal(value * MovementSpeed * deltaTime);
        Transform.Rotation = QuatMakers.FromRotationRadians(0, difference, 0);
    }
    
    [OnInput(InputAction.Jump)]
    protected void OnJump()
    {
        if (Transform.Position.Y > 0)
            return;
        PhysicsComponent.Velocity.Y = 20.0;
    }
}
