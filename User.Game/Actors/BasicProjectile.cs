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
}