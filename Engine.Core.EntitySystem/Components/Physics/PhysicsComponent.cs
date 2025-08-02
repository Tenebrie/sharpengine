using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Services;

namespace Engine.Core.EntitySystem.Components.Physics;

public partial class PhysicsComponent : ActorComponent
{
    public Vector3 Velocity = Vector3.Zero;

    [OnInit]
    protected void OnRegisterOnPhysicsServer()
    {
        GetService<PhysicsService>().Register(this);
    }
    
    [OnDestroy]
    protected void OnUnregisterOnPhysicsServer()
    {
        GetService<PhysicsService>().Unregister(this);
    }
    
    [OnUpdate]
    protected void OnUpdate(double deltaTime)
    {
        if (Actor.Transform.Position.Y > 0)
        {
            Velocity.Y -= 35.0 * deltaTime;
        }
        
        Actor.Transform.TranslateGlobal(Velocity * deltaTime);
        if (Actor.Transform.Position.Y > 0)
            return;
        
        Actor.Transform.Position = new Vector3(Actor.Transform.Position.X, 0, Actor.Transform.Position.Z);
        Velocity.Y = 0;
    }
}
