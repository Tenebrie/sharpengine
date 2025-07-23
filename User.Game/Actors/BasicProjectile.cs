using Engine.Assets.Loaders;
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
        ObjMeshLoader.LoadObj("Assets/Meshes/projectile-sword.obj", out var vertices, out var indices);
        MeshComponent.Mesh.Load(vertices, indices);
        MeshComponent.Material = new AlliedProjectile();
        MeshComponent.Transform.Rotation = QuatMakers.FromRotation(0, -90, 0);
    }
}