using Engine.Core.Common;
using Engine.Core.Makers;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Physics;
using Engine.Core.EntitySystem.Entities;
using User.Game.Actors;

namespace User.Game.Player;

public partial class PlayerCameraFollower : Actor
{
    [Component] public MainCamera MainCameraComponent;
    [Component] public PhysicsComponent Physics;
    
    public PlayerCharacter PlayerCharacter { get; set; }

    [OnReady]
    protected void OnReady()
    {
        MainCameraComponent.Transform.Position = new Vector3(0, 150, 0);
        MainCameraComponent.Transform.Rotation = QuatMakers.FromRotation(75, 0, 0);
    }
    
    [OnUpdate]
    protected void OnUpdate(double deltaTime)
    {
        // Transform.Position += (PlayerCharacter.Transform.Position - Transform.Position) * deltaTime * 3.0f;
        Physics.LinearVelocity = (PlayerCharacter.Transform.Position - Transform.Position) * 3.0f;
    }
}