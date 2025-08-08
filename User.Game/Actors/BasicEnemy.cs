using Engine.Core.Communication.Groups;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Physics;
using Engine.Core.EntitySystem.Entities;

namespace User.Game.Actors;

public partial class BasicEnemy : ActorInstance
{
    [DefaultGroup] public static readonly Group<BasicEnemy> All = new(); 
    [Component] public PhysicsComponent Physics;
    [Component] public ColliderSphereComponent ColliderSphere;

    public double Health { get; set; } = 100.0;

    public void DealDamage(double damage)
    {
        Health -= damage;
        if (Health <= 0) 
            QueueFree(); 
    }

    [OnReady]
    protected void OnReady()
    {
        ColliderSphere.Radius = 0.5;
    }
}