using Engine.Assets;
using Engine.Assets.Materials.Meshes.AlliedProjectile;
using Engine.Core.Makers;
using Engine.Worlds.Attributes;
using Engine.Worlds.Components;
using Engine.Worlds.Entities;

namespace User.Game.Actors;

public class BasicProjectile : Actor
{
    [Component] protected StaticMeshComponent MeshComponent;
    [Component] public PhysicsComponent PhysicsComponent;
    
    [OnInit]
    protected void OnInit()
    {
        MeshComponent.Mesh = AssetManager.LoadMesh("Assets/Meshes/projectile-sword.obj");
        MeshComponent.BoundingSphere.Generate(MeshComponent.Mesh.Vertices);
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