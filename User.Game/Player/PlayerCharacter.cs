using Engine.Core.Extensions;
using Engine.Core.Makers;
using Engine.Worlds.Attributes;
using Engine.Worlds.Entities;
using User.Game.Player.Components;
using User.Game.Services;
using Axis = Engine.Core.Common.Axis;

namespace User.Game.Player;

public class PlayerCharacter : Actor
{
    private const double MovementSpeed = 50.0;
    private const double RotationSpeed = 0.12;
    
    [Component] public DragonMesh DragonMeshComponent;

    [OnInputHeld(InputAction.MoveForward,  +1.0, +0.0)]
    [OnInputHeld(InputAction.MoveBackward, -1.0, -0.0)]
    [OnInputHeld(InputAction.MoveLeft,     +0.0, -1.0)]
    [OnInputHeld(InputAction.MoveRight,    +0.0, +1.0)]
    protected void OnMove(double deltaTime, Vector2 direction)
    {
        if (direction.LengthSquared == 0)
            return;
        var value = new Vector(direction.Y, 0, -direction.X).Normalized();
        var forwardVector = Axis.Forward;
        var dotProduct = value.Dot(forwardVector);
        var crossProduct = value.Cross(forwardVector);
        var difference = Math.Atan2(crossProduct.Y, dotProduct);
        Transform.TranslateGlobal(value * MovementSpeed * deltaTime);
        Transform.Rotation = QuatMakers.FromRotationRadians(0, difference, 0);
    }
}