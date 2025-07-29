using Engine.Core.Common;
using Engine.Core.Logging;
using Engine.Worlds.Attributes;
using Engine.Worlds.Entities;

namespace Engine.Worlds.Components;

public class PhysicsComponent : ActorComponent
{
    public Vector3 Velocity = Vector3.Zero;
    
    [OnUpdate]
    protected void OnUpdate(double deltaTime)
    {
        if (Actor.Transform.Position.Y > 0)
        {
            Velocity.Y -= 35.0 * deltaTime;
        }
        
        Actor.Transform.TranslateGlobal(Velocity * deltaTime);
        if (Actor.Transform.Position.Y <= 0)
        {
            Actor.Transform.Position = new Vector3(Actor.Transform.Position.X, 0, Actor.Transform.Position.Z);
            Velocity.Y = 0;
        }
    }
}