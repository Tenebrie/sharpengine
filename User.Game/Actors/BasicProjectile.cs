using System.Drawing;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.Common;
using Engine.Core.Communication.Groups;
using Engine.Core.Communication.Signals;
using Engine.Core.Makers;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Physics;
using Engine.Core.EntitySystem.Components.Rendering;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Logging;
using User.Game.Actors.BasicEnemies;

namespace User.Game.Actors;

public partial class BasicProjectile : Actor
{
    [Signal] public static readonly Signal<BasicProjectile> ProjectileCreated;
    [DefaultGroup] public static readonly Group<BasicProjectile> All = new();
    
    [Component] public PhysicsComponent PhysicsComponent;
    [Component] public StaticMeshComponent MeshComponent;
    
    private readonly List<BasicEnemy> _enemiesHit = [];
    
    public double Damage { get; set; } = 100.0;
    public int MaxPierce { get; set; } = 1;
    
    [OnReady]
    protected void OnReady()
    {
        ProjectileCreated.Emit(this);
        MeshComponent.StaticMesh = StaticMesh.CreateFromDisk("Meshes/projectile-sword.obj");
        MeshComponent.MaterialInstance = Material.CreateCachedFromDisk("Shaders/cube").Instantiate().SetTintColor(Color.Red);
        MeshComponent.Transform.Rotation = QuatMakers.FromRotation(0, -90, 0);
        Transform.Rescale(0.1);
    }

    // [OnTimer(Seconds = 0.05f)]
    // protected void CheckCollision()
    // {
    //     foreach (var enemy in ParentScene.Actors.OfType<BasicEnemy>()
    //                  .Where(enemy => !_enemiesHit.Contains(enemy))
    //                  .Where(enemy => !enemy.IsDying)
    //                  .Where(enemy => enemy.Transform.Position.DistanceTo(Transform.Position) <= MeshComponent.BoundingSphere.WorldRadius + 3))
    //     {
    //         enemy.DealDamage(Damage);
    //         _enemiesHit.Add(enemy);
    //         if (_enemiesHit.Count >= MaxPierce)
    //         {
    //             QueueFree();
    //             return;
    //         }
    //     }
    // }
    
    [OnUpdate]
    protected void OnUpdate(double deltaTime)
    {
        if (_isFadingOut)
        {
            MeshComponent.MaterialInstance.Tint.W -= (float)(deltaTime * 0.5);
        }
        if (Transform.Position.Y <= -2)
        {
            PhysicsComponent.LinearVelocity = Vector3.Zero;
            IsTicking = false;
            return;
        }
    }
    
    private bool _isFadingOut = false;
    
    // [OnTimer(Seconds = 5.0f, TicksOnce = true)]
    // protected void TimeoutFadeStart()
    // {
    //     _isFadingOut = true;
    // }
    //
    [OnTimer(Seconds = 4.0f)]
    protected void TimeoutDestroy()
    {
        QueueFree();
    }
}