using Engine.Core.Assets;
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
    [Component] protected StaticMeshComponent MeshComponent;
    [Component] public PhysicsComponent PhysicsComponent;
    
    [OnInit]
    protected void OnInit()
    {
        ProjectileCreated.Emit(this);
        MeshComponent.Mesh = AssetManager.LoadMesh("Assets/Meshes/projectile-sword.obj");
        MeshComponent.Material = AssetManager.LoadMaterial("Meshes/AlliedProjectile/AlliedProjectile");
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