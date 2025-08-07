using Engine.Core.Assets;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.Communication.Signals;
using Engine.Core.Makers;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Physics;
using Engine.Core.EntitySystem.Components.Rendering;
using Engine.Core.EntitySystem.Entities;

namespace User.Game.Actors;

public partial class BasicProjectile : Actor
{
    [Signal] public static readonly Signal<BasicProjectile> ProjectileCreated;
    [Component] public PhysicsComponent PhysicsComponent;
    [Component] protected StaticMeshComponent MeshComponent;
    
    [OnReady]
    protected void OnReady()
    {
        ProjectileCreated.Emit(this);
        MeshComponent.Mesh = StaticMesh.CreateFromDisk("Meshes/projectile-sword.obj");
        MeshComponent.Material = Material.CreateFromDisk("Meshes/AlliedProjectile/AlliedProjectile").Instantiate();
        MeshComponent.Transform.Rotation = QuatMakers.FromRotation(0, -90, 0);
    }

    [OnTimer(Seconds = 0.05f)]
    protected void CheckCollision()
    {
        foreach (var enemy in ParentScene.Actors.OfType<BasicEnemy>()
                     .Where(enemy => enemy.Transform.Position.DistanceTo(Transform.Position) <= MeshComponent.BoundingSphere.WorldRadius + 3))
        {
            enemy.DealDamage(100.0);
            QueueFree();
            return;
        }
    }
    
    [OnTimer(Seconds = 1.0f)]
    protected void TimeoutDestroy()
    {
        QueueFree();
    }
}