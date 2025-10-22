using System.Drawing;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.Common;
using Engine.Core.Communication.Signals;
using Engine.Core.Makers;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Physics;
using Engine.Core.EntitySystem.Components.Rendering;
using Engine.Core.EntitySystem.Entities;
using User.Game.Actors.BasicEnemies;

namespace User.Game.Actors;

public partial class CleaningProjectile : Actor
{
    [Component] public PhysicsComponent PhysicsComponent;
    [Component] protected StaticMeshComponent MeshComponent;
    
    [OnReady]
    protected void OnReady()
    {
        MeshComponent.StaticMesh = StaticMesh.CreateFromDisk("Meshes/projectile-sword.obj");
        MeshComponent.MaterialInstance = Material.CreateCachedFromDisk("Shaders/cube").Instantiate().SetTintColor(Color.CornflowerBlue);
        MeshComponent.Transform.Rotation = QuatMakers.FromRotation(0, -90, 0);
        Transform.Rescale(0.1);
    }
    
    [OnUpdate]
    protected void OnUpdate(double deltaTime)
    {
        if (Transform.Position.Y > -2)
            return;
        PhysicsComponent.LinearVelocity = Vector3.Zero;
        IsTicking = false;
        OnClean();
        QueueFree();
    }

    private void OnClean()
    {
        // Delete all sword in a radius
        var targets = BasicProjectile.All.Where(proj =>
            proj.WorldTransform.Position.DistanceTo(WorldTransform.Position) < 0.7).ToList();
        foreach (var enemy in targets)
        {
            enemy.QueueFree();
        }
    }
    
    [OnTimer(Seconds = 4.0f)]
    protected void TimeoutDestroy()
    {
        QueueFree();
    }
}