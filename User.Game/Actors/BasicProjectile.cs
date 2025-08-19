using Engine.Core.Assets;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.Communication.Signals;
using Engine.Core.Makers;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Physics;
using Engine.Core.EntitySystem.Components.Rendering;
using Engine.Core.EntitySystem.Entities;
using User.Game.Actors.BasicEnemies;

namespace User.Game.Actors;

public partial class BasicProjectile : Actor
{
    [Signal] public static readonly Signal<BasicProjectile> ProjectileCreated;
    [Component] public PhysicsComponent PhysicsComponent;
    [Component] protected StaticMeshComponent MeshComponent;
    
    private readonly List<BasicEnemy> _enemiesHit = [];
    
    [OnReady]
    protected void OnReady()
    {
        ProjectileCreated.Emit(this);
        MeshComponent.StaticMesh = StaticMesh.CreateFromDisk("Meshes/projectile-sword.obj");
        MeshComponent.MaterialInstance = Material.CreateFromDisk("Shaders/cube").Instantiate();
        MeshComponent.Transform.Rotation = QuatMakers.FromRotation(0, -90, 0);
    }

    [OnTimer(Seconds = 0.05f)]
    protected void CheckCollision()
    {
        foreach (var enemy in ParentScene.Actors.OfType<BasicEnemy>()
                     .Where(enemy => !_enemiesHit.Contains(enemy))
                     .Where(enemy => enemy.Transform.Position.DistanceTo(Transform.Position) <= MeshComponent.BoundingSphere.WorldRadius + 3))
        {
            enemy.DealDamage(100.0);
            _enemiesHit.Add(enemy);
            if (_enemiesHit.Count >= 3)
            {
                QueueFree();
                return;
            }
        }
    }
    
    [OnTimer(Seconds = 2.0f)]
    protected void TimeoutDestroy()
    {
        QueueFree();
    }
}